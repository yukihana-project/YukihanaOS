// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class RmdirCommand : IShellCommand
{
    public string Name => "rmdir";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Remove an empty directory";
    public string Usage => "rmdir <path>";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 1)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string target = context.Arguments[0];
        string path = context.Session.ResolvePath(target);

        if (!VfsManager.TryStat(path, out VfsStat stat))
        {
            return new ShellCommandResult.Error($"rmdir: {target}: no such directory");
        }

        if (!stat.IsDirectory)
        {
            return new ShellCommandResult.Error($"rmdir: {target}: not a directory");
        }

        if (!FsPermissions.Allows(ShellSession.Parent(path), FsAccessMode.Write))
        {
            return new ShellCommandResult.PermissionDenied($"rmdir: {target}: permission denied");
        }

        return VfsManager.TryRemoveDirectory(path)
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error($"rmdir: {target}: directory is not empty or cannot be removed");
    }
}
