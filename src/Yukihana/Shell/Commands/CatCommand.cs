// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class CatCommand : IShellCommand
{
    public string Name => "cat";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Print files, or input when no files are given";
    public string Usage => "cat [file...]";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count == 0)
        {
            string? line;
            while ((line = context.In.ReadLine()) is not null)
            {
                context.Out.WriteLine(line);
            }

            return ShellCommandResult.Ok;
        }

        foreach (string arg in context.Arguments)
        {
            string path = context.Session.ResolvePath(arg);

            if (!File.Exists(path))
            {
                return new ShellCommandResult.Error($"cat: {arg}: no such file");
            }

            if (!FsPermissions.Allows(path, FsAccessMode.Read))
            {
                return new ShellCommandResult.PermissionDenied($"cat: {arg}: permission denied");
            }

            context.Out.Write(File.ReadAllText(path));
        }

        return ShellCommandResult.Ok;
    }
}
