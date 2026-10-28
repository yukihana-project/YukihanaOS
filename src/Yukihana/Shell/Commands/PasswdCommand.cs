// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Extensions.System;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class PasswdCommand : IShellCommand
{
    public string Name => "passwd";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Change a user's password";
    public string Usage => "passwd [user]";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count > 1)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        SecurityContext security = Kernel.SecurityManager.Current;
        UserId self = security.EffectiveUser;

        User? target = context.Arguments.Count == 0
            ? UserManager.GetUser(self)
            : UserManager.GetUser(context.Arguments[0]);

        if (target is null)
        {
            return new ShellCommandResult.Error("passwd: unknown user");
        }

        bool canManage = security.HasCapability(Capability.ManageUsers);

        if (!canManage)
        {
            if (target.Id != self)
            {
                return new ShellCommandResult.PermissionDenied($"passwd: {target.Name}: permission denied");
            }

            Console.Write("Current password: ");
            string? current = Console.ReadLineHidden();

            if (!AccountManager.Authenticate(target.Id, current ?? ""))
            {
                return new ShellCommandResult.Error("passwd: authentication failure");
            }
        }

        AccountManager.SetPassword(target, PasswordPrompt.ReadNew());

        return UserDatabase.Save()
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error("passwd: changed in memory, but could not save /etc/passwd");
    }
}
