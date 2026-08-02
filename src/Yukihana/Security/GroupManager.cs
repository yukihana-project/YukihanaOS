// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;

namespace Yukihana.Security;

public static class GroupManager
{
    public static uint NextGid { get; set; } = 1000;

    private static readonly ConcurrentDictionary<GroupId, Group> s_groups = new()
    {
        [GroupId.Root] = new Group
        {
            Id = GroupId.Root,
            Name = "root"
        }
    };

    public static void AddGroup(string name)
    {
        GroupId gid = new(NextGid++);
        s_groups.TryAdd(gid, new Group()
        {
            Id = gid,
            Name = name
        });
    }

    public static void RemoveGroup(Group group)
    {
        s_groups.TryRemove(group.Id, out _);
    }

    public static Group? GetGroup(GroupId gid)
    {
        return s_groups.TryGetValue(gid, out Group? group) ? group : null;
    }

    public static Group? GetGroup(string name)
    {
        return s_groups.Values.FirstOrDefault(g => g.Name == name);
    }
}
