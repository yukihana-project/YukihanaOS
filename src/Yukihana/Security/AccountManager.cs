// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Yukihana.Debug;

namespace Yukihana.Security;

public static class AccountManager
{
    private static readonly ConcurrentDictionary<UserId, UserAccount> s_accounts = [];

    public static void Reset() => s_accounts.Clear();

    public static void Restore(User user, PasswordHash password, bool locked)
    {
        s_accounts[user.Id] = new UserAccount
        {
            User = user,
            Password = password,
            Locked = locked
        };
    }

    public static void SetPassword(User user, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        PasswordHash hash = new(
            salt,
            Pbkdf2Sha256.DeriveKey(Encoding.UTF8.GetBytes(password), salt),
            PasswordHashAlgorithm.Pbkdf2Sha256);

        s_accounts.AddOrUpdate(
            user.Id,
            _ => new UserAccount { User = user, Password = hash },
            (_, account) =>
            {
                account.Password = hash;
                return account;
            });
    }

    public static void Remove(User user) => s_accounts.Remove(user.Id, out _);

    public static UserAccount? Get(User user) => s_accounts.GetValueOrDefault(user.Id);
    public static UserAccount? Get(UserId user) => s_accounts.GetValueOrDefault(user);

    public static void Lock(User user)
    {
        UserAccount? account = Get(user);
        UserAccount? updAccount = account;

        if (updAccount is null || account is null)
        {
            return;
        }

        updAccount.Locked = true;

        s_accounts.TryUpdate(user.Id, updAccount, account);
    }

    public static void Unlock(User user)
    {
        UserAccount? account = Get(user);
        UserAccount? updAccount = account;

        if (updAccount is null || account is null)
        {
            return;
        }

        updAccount.Locked = false;

        s_accounts.TryUpdate(user.Id, updAccount, account);
    }

    public static bool Authenticate(UserId user, string password)
    {
        Logger.GlobalLogger.Trace("Authenticate(User, string) called");
        UserAccount? account = Get(user);

        if (account is null)
        {
            return false;
        }

        Logger.GlobalLogger.Trace("Account is not null");

        if (account.Locked)
        {
            return false;
        }

        Logger.GlobalLogger.Trace("Account is not locked");

        PasswordHashAlgorithm algorithm = account.Password.UsedAlgorithm;

        bool Pbkdf2Sha256Equal = account.Password.Hash.SequenceEqual(Pbkdf2Sha256.DeriveKey(
            Encoding.UTF8.GetBytes(password), account.Password.Salt));

        Logger.GlobalLogger.Trace($"Hashes eauql? {Pbkdf2Sha256Equal}");

        return algorithm switch
        {
            PasswordHashAlgorithm.Pbkdf2Sha256 => Pbkdf2Sha256Equal,
            _ => false
        };
    }
}
