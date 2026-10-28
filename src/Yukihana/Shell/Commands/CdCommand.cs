// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class CdCommand : IShellCommand
{
    public string Name => "cd";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Change the current directory";
    public string Usage => "cd [path]";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.Builtin | ShellCommandFlags.MutatesShellState;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        string target = context.Arguments.Count > 0 ? context.Arguments[0] : "/";
        string path = context.Session.ResolvePath(target);

        if (!VfsManager.TryStat(path, out VfsStat stat) || !stat.IsDirectory)
        {
            return new ShellCommandResult.Error($"cd: {target}: no such directory");
        }

        if (!FsPermissions.Allows(path, FsAccessMode.Execute))
        {
            return new ShellCommandResult.PermissionDenied($"cd: {target}: permission denied");
        }

        context.Session.CurrentDirectory = path;
        return ShellCommandResult.Ok;
    }
}
