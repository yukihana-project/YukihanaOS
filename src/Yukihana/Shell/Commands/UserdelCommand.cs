// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class UserdelCommand : IShellCommand
{
    public string Name => "userdel";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Delete a user account";
    public string Usage => "userdel <name>";
    public Capability RequiredCapabilities => Capability.ManageUsers;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 1)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        User? target = UserManager.GetUser(context.Arguments[0]);

        if (target is null)
        {
            return new ShellCommandResult.Error("userdel: unknown user");
        }

        if (target.Id == UserId.Root)
        {
            return new ShellCommandResult.Error("userdel: cannot delete root");
        }

        if (target.Id == Kernel.SecurityManager.Current.EffectiveUser)
        {
            return new ShellCommandResult.Error("userdel: cannot delete the current user");
        }

        UserManager.RemoveUser(target);
        AccountManager.Remove(target);

        return UserDatabase.Save()
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error("userdel: removed in memory, but could not save /etc/passwd");
    }
}
