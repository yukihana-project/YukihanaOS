// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class EchoCommand : IShellCommand
{
    public string Name => "echo";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Write arguments to output";
    public string Usage => "echo [text...]";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        context.Out.WriteLine(string.Join(' ', context.Arguments));
        return ShellCommandResult.Ok;
    }
}
