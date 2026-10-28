// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class GroupaddCommand : IShellCommand
{
    public string Name => "groupadd";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Create a group";
    public string Usage => "groupadd <name>";
    public Capability RequiredCapabilities => Capability.ManageUsers;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 1)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string name = context.Arguments[0];

        if (!UserDatabase.IsValidName(name))
        {
            return new ShellCommandResult.Error($"groupadd: invalid group name '{name}'");
        }

        if (GroupManager.GetGroup(name) is not null)
        {
            return new ShellCommandResult.Error($"groupadd: group '{name}' already exists");
        }

        GroupManager.AddGroup(name);

        return GroupDatabase.Save()
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error("groupadd: created in memory, but could not save /etc/group");
    }
}
