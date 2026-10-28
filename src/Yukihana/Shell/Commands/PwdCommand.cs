// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class PwdCommand : IShellCommand
{
    public string Name => "pwd";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Print the current directory";
    public string Usage => "pwd";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        context.Out.WriteLine(context.Session.CurrentDirectory);
        return ShellCommandResult.Ok;
    }
}
