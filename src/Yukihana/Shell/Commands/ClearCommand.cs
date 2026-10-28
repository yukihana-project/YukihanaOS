// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class ClearCommand : IShellCommand
{
    public string Name => "clear";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Clear the screen";
    public string Usage => "clear";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        // shortcut: Console.Clear() is unverified on Cosmos, check on a VM
        Console.Clear();
        return ShellCommandResult.Ok;
    }
}
