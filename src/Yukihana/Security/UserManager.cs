// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public static class UserManager
{
    public static uint NextUid { get; set; } = 1000;

    private static readonly List<User> s_users =
    [
        new()
        {
            DefaultCapabilities = CapabilitySet.Root,
            Enabled = true,
            Id = UserId.Root,
            Name = "root",
            PrimaryGroup = GroupId.Root,
            SupplementaryGroups = []
        }
    ];

    public static void AddUser(string name, CapabilitySet capabilies, GroupId primaryGroupId, bool isEnabled = true)
    {
        s_users.Add(new User()
        {
            Id = new UserId(NextUid++),
            Name = name,
            DefaultCapabilities = capabilies,
            PrimaryGroup = primaryGroupId,
            Enabled = isEnabled,
            SupplementaryGroups = []
        });
    }

    public static void RemoveUser(User user)
    {
        s_users.Remove(user);
    }

    public static User? GetUser(UserId uid)
    {
        return s_users.FirstOrDefault(u => u.Id == uid);
    }

    public static User? GetUser(string name)
    {
        return s_users.FirstOrDefault(u => u.Name == name);
    }
}
