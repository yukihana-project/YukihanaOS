// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Extensions.System;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class SudoCommand(ShellCommandRegistry registry) : IShellCommand
{
    private const string SudoersGroup = "wheel";

    public string Name => "sudo";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Run one command as root";
    public string Usage => "sudo <command> [args...]";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count == 0)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        SecurityContext current = Kernel.SecurityManager.Current;

        if (current.RealUser != UserId.Root)
        {
            ShellCommandResult? denied = CheckInvoker(current);

            if (denied is not null)
            {
                return denied;
            }
        }

        // The real user stays the same, so the audit trail still shows who ran sudo
        Kernel.SecurityManager.SetCurrent(current.Elevate());

        try
        {
            ShellCommandDispatcher dispatcher = new(registry);
            ShellCommandLine line = new(context.Arguments[0], context.Arguments.Skip(1).ToArray());

            return dispatcher.DispatchCommand(context.Session, line, context.In, context.Out);
        }
        finally
        {
            Kernel.SecurityManager.SetCurrent(current);
        }
    }

    private static ShellCommandResult? CheckInvoker(SecurityContext current)
    {
        User? invoker = UserManager.GetUser(current.RealUser);
        Group? sudoers = GroupManager.GetGroup(SudoersGroup);

        if (invoker is null ||
            sudoers is null ||
            (invoker.PrimaryGroup != sudoers.Id && !invoker.SupplementaryGroups.Contains(sudoers.Id)))
        {
            return new ShellCommandResult.PermissionDenied($"sudo: {invoker?.Name ?? "unknown user"} is not in the sudoers group");
        }

        Console.Write($"[sudo] password for {invoker.Name}: ");
        string? password = Console.ReadLineHidden();

        if (!AccountManager.Authenticate(invoker.Id, password ?? ""))
        {
            return new ShellCommandResult.Error("sudo: incorrect password");
        }

        return null;
    }
}
