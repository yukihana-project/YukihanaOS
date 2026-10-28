// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class ShutdownCommand : IShellCommand
{
    public string Name => "shutdown";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Power off the machine";
    public string Usage => "shutdown";
    public Capability RequiredCapabilities => Capability.Shutdown;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        Cosmos.Kernel.System.Power.Shutdown();
        return ShellCommandResult.Ok;
    }
}
