// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;
using System.Text;
using acryptohashnet;
using Yukihana.Debug;

namespace Yukihana.Security;

public static class AccountManager
{
    private static readonly ConcurrentDictionary<UserId, UserAccount> s_accounts = [];

    static AccountManager()
    {
        string saltStr = $"0|root|{DateTime.Now.ToBinary()}";
        SHA256 sha = new();
        byte[] salt = sha.ComputeHash(Encoding.UTF8.GetBytes(saltStr));
        byte[] hash = Pbkdf2Sha256.DeriveKey([.. "root"u8], salt);

        s_accounts.TryAdd(UserId.Root,
            new UserAccount
            {
                Locked = false,
                Password = new PasswordHash(salt, hash, PasswordHashAlgorithm.Pbkdf2Sha256),
                PasswordExpires = DateTimeOffset.MaxValue,
                User = User.Root
            });
    }

    public static void Add(
        User user,
        string password,
        bool doLock = false,
        DateTimeOffset? passwordExpires = null,
        PasswordHashAlgorithm algorithm = PasswordHashAlgorithm.Pbkdf2Sha256)
    {
        passwordExpires ??= DateTimeOffset.MaxValue;

        if (!s_accounts.ContainsKey(user.Id))
        {
            return;
        }

        switch (algorithm)
        {
            case PasswordHashAlgorithm.Pbkdf2Sha256:
            {
                SHA256 sha = new();

                string saltString = $"{user.Name}|{user.Id.Value}|{DateTime.Now.ToBinary()}";

                byte[] salt = sha.ComputeHash(Encoding.UTF8.GetBytes(saltString));

                s_accounts.TryAdd(user.Id, new UserAccount()
                {
                    User = user,
                    Password = new PasswordHash(
                        salt,
                        Pbkdf2Sha256.DeriveKey(
                            Encoding.UTF8.GetBytes(password),
                            salt),
                        algorithm),
                    Locked = doLock,
                    PasswordExpires = passwordExpires
                });
            }
            break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
        }
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
