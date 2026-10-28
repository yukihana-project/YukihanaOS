// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class UsermodCommand : IShellCommand
{
    public string Name => "usermod";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Add a user to a supplementary group";
    public string Usage => "usermod -aG <group> <user>";
    public Capability RequiredCapabilities => Capability.ManageUsers;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 3 || context.Arguments[0] != "-aG")
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        Group? group = GroupManager.GetGroup(context.Arguments[1]);

        if (group is null)
        {
            return new ShellCommandResult.Error("usermod: unknown group");
        }

        User? target = UserManager.GetUser(context.Arguments[2]);

        if (target is null)
        {
            return new ShellCommandResult.Error("usermod: unknown user");
        }

        target.SupplementaryGroups = target.SupplementaryGroups.Add(group.Id);

        return UserDatabase.Save()
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error("usermod: changed in memory, but could not save /etc/passwd");
    }
}
