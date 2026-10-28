// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;
using Yukihana.Debug;
using Yukihana.Vfs;

namespace Yukihana.Security;

// One group per line: name:gid
// Group membership is stored per user in /etc/passwd
public static class GroupDatabase
{
    public const string DatabasePath = "/etc/group";

    public static void Load(string text)
    {
        GroupManager.Reset();

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] fields = line.Split(':');

            if (fields.Length != 2 ||
                !UserDatabase.IsValidName(fields[0]) ||
                !uint.TryParse(fields[1], out uint gid))
            {
                Logger.GlobalLogger.Warn($"Skipping malformed line in {DatabasePath}");
                continue;
            }

            GroupManager.Restore(new Group { Id = new GroupId(gid), Name = fields[0] });
        }
    }

    public static bool Save()
    {
        StringBuilder sb = new();
        sb.AppendLine("# name:gid");

        foreach (Group group in GroupManager.All)
        {
            sb.Append(group.Name).Append(':').Append(group.Id.Value).Append('\n');
        }

        return VfsFile.Write(DatabasePath, sb.ToString(), VfsFile.PrivateFileMode);
    }
}
