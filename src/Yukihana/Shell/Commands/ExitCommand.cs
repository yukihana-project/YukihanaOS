// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class ExitCommand : IShellCommand
{
    public string Name => "exit";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "End the session and return to login";
    public string Usage => "exit";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.Builtin | ShellCommandFlags.MutatesShellState;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        context.Session.ExitRequested = true;
        return ShellCommandResult.Ok;
    }
}
