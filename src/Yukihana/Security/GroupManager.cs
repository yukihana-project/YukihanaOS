// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;

namespace Yukihana.Security;

public static class GroupManager
{
    public static uint NextGid { get; set; } = 1000;

    private static readonly ConcurrentDictionary<GroupId, Group> s_groups = [];

    static GroupManager()
    {
        Reset();
    }

    public static IEnumerable<Group> All => s_groups.Values;

    public static void Reset()
    {
        s_groups.Clear();
        Restore(new Group { Id = GroupId.Root, Name = "root" });
        Restore(new Group { Id = new GroupId(10), Name = "wheel" });
        Restore(new Group { Id = new GroupId(1000), Name = "users" });
        NextGid = 1001;
    }

    public static void Restore(Group group)
    {
        s_groups[group.Id] = group;
        NextGid = Math.Max(NextGid, group.Id.Value + 1);
    }

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
