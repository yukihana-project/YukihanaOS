// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class HelpCommand(ShellCommandRegistry registry) : IShellCommand
{
    public string Name => "help";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "List available commands";
    public string Usage => "help";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        foreach (IShellCommand command in registry.All.OrderBy(c => c.Name, StringComparer.Ordinal))
        {
            context.Out.WriteLine($"{command.Name,-10} {command.Summary}");
        }

        return ShellCommandResult.Ok;
    }
}
