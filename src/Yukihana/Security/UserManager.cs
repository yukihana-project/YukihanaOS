// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;
using Yukihana.Debug;

namespace Yukihana.Security;

public static class UserManager
{
    public static uint NextUid { get; set; } = 1000;

    private static readonly ConcurrentDictionary<UserId, User> s_users = new()
    {
        [UserId.Root] = User.Root
    };

    public static IEnumerable<User> All => s_users.Values;

    public static void AddUser(string name, CapabilitySet capabilies, GroupId primaryGroupId, bool isEnabled = true)
    {
        UserId uid = new(NextUid++);
        s_users.TryAdd(uid, new User()
        {
            Id = uid,
            Name = name,
            DefaultCapabilities = capabilies,
            PrimaryGroup = primaryGroupId,
            Enabled = isEnabled,
            SupplementaryGroups = []
        });
    }

    public static void Restore(User user)
    {
        s_users[user.Id] = user;
        NextUid = Math.Max(NextUid, user.Id.Value + 1);
    }

    public static void Reset()
    {
        s_users.Clear();
        s_users[UserId.Root] = User.Root;
        NextUid = 1000;
    }

    public static void RemoveUser(User user)
    {
        s_users.TryRemove(user.Id, out _);
    }

    public static User? GetUser(UserId uid)
    {
        return s_users.TryGetValue(uid, out User? user) ? user : null;
    }

    public static User? GetUser(string name)
    {
        Logger.GlobalLogger.Debug($"Trying to poll user with name {name}");
        return s_users.Values.FirstOrDefault(u => u.Name == name);
    }
}
