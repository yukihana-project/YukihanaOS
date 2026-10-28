// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Immutable;
using System.Text;
using Yukihana.Debug;
using Yukihana.Vfs;

namespace Yukihana.Security;

// One user per line: name:uid:gid:groups:capabilities:enabled:locked:salt:hash
// groups is a comma separated list of gids, salt and hash are hex, both empty when the user has no password
public static class UserDatabase
{
    public const string DatabasePath = "/etc/passwd";

    public static bool IsValidName(string name)
    {
        return name.Length is >= 1 and <= 32
            && (char.IsAsciiLetterLower(name[0]) || name[0] == '_')
            && name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '_' or '-');
    }

    public static void Load(string text)
    {
        UserManager.Reset();
        AccountManager.Reset();

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (!TryLoadLine(line))
            {
                Logger.GlobalLogger.Warn($"Skipping malformed line in {DatabasePath}");
            }
        }
    }

    public static bool Save() => VfsFile.Write(DatabasePath, Serialize(), VfsFile.PrivateFileMode);

    private static string Serialize()
    {
        StringBuilder sb = new();
        sb.AppendLine("# name:uid:gid:groups:capabilities:enabled:locked:salt:hash");

        foreach (User user in UserManager.All)
        {
            UserAccount? account = AccountManager.Get(user);

            sb.Append(user.Name).Append(':')
              .Append(user.Id.Value).Append(':')
              .Append(user.PrimaryGroup.Value).Append(':')
              .AppendJoin(',', user.SupplementaryGroups.Select(g => g.Value)).Append(':')
              .Append((ulong)user.DefaultCapabilities.Value).Append(':')
              .Append(user.Enabled ? '1' : '0').Append(':')
              .Append(account?.Locked == true ? '1' : '0').Append(':')
              .Append(account is null ? "" : Convert.ToHexString(account.Password.Salt)).Append(':')
              .Append(account is null ? "" : Convert.ToHexString(account.Password.Hash))
              .Append('\n');
        }

        return sb.ToString();
    }

    private static bool TryLoadLine(string line)
    {
        string[] fields = line.Split(':');

        if (fields.Length != 9 ||
            !IsValidName(fields[0]) ||
            !uint.TryParse(fields[1], out uint uid) ||
            !uint.TryParse(fields[2], out uint gid) ||
            !ulong.TryParse(fields[4], out ulong capabilities) ||
            !TryParseGroups(fields[3], out ImmutableHashSet<GroupId> groups) ||
            !TryParseHex(fields[7], out byte[] salt) ||
            !TryParseHex(fields[8], out byte[] hash))
        {
            return false;
        }

        bool locked = fields[6] == "1";
        PasswordHash? password = salt.Length > 0 && hash.Length > 0
            ? new PasswordHash(salt, hash, PasswordHashAlgorithm.Pbkdf2Sha256)
            : null;

        // Root keeps its built-in definition, the file only supplies its password
        if (uid == UserId.Root.Value)
        {
            if (password is not null)
            {
                AccountManager.Restore(User.Root, password, locked);
            }

            return true;
        }

        User user = new()
        {
            Id = new UserId(uid),
            Name = fields[0],
            PrimaryGroup = new GroupId(gid),
            SupplementaryGroups = groups,
            DefaultCapabilities = new CapabilitySet((Capability)capabilities),
            Enabled = fields[5] == "1"
        };

        UserManager.Restore(user);

        if (password is not null)
        {
            AccountManager.Restore(user, password, locked);
        }

        return true;
    }

    private static bool TryParseGroups(string text, out ImmutableHashSet<GroupId> groups)
    {
        groups = ImmutableHashSet<GroupId>.Empty;

        if (text.Length == 0)
        {
            return true;
        }

        List<GroupId> list = [];

        foreach (string part in text.Split(','))
        {
            if (!uint.TryParse(part, out uint gid))
            {
                return false;
            }

            list.Add(new GroupId(gid));
        }

        groups = list.ToImmutableHashSet();
        return true;
    }

    private static bool TryParseHex(string text, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(text);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
