// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public static class GroupManager
{
    public static uint NextGid { get; set; } = 1000;

    private static readonly List<Group> s_groups =
    [
        new()
        {
            Id = new GroupId(0),
            Name = "root"
        }
    ];

    public static void AddGroup(string name)
    {
        s_groups.Add(new Group()
        {
            Id = new GroupId(NextGid++),
            Name = name
        });
    }

    public static void RemoveGroup(Group group)
    {
        s_groups.Remove(group);
    }

    public static Group? GetGroup(GroupId gid)
    {
        return s_groups.FirstOrDefault(g => g.Id == gid);
    }

    public static Group? GetGroup(string name)
    {
        return s_groups.FirstOrDefault(g => g.Name == name);
    }
}
