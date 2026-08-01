// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;

namespace Yukihana.Security;

public static class UserManager
{
    public static uint NextUid { get; set; } = 1000;

    private static readonly ConcurrentDictionary<UserId, User> s_users = new()
    {
        [UserId.Root] = User.Root
    };

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
        return s_users.Values.FirstOrDefault(u => u.Name == name);
    }
}
