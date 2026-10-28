// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;
using Yukihana.Vfs;

namespace Yukihana.Shell.Commands;

public sealed class MkdirCommand : IShellCommand
{
    public string Name => "mkdir";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Create a directory";
    public string Usage => "mkdir <path>";
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

        if (VfsManager.TryStat(path, out VfsStat _))
        {
            return new ShellCommandResult.Error($"mkdir: {target}: already exists");
        }

        if (!FsPermissions.Allows(ShellSession.Parent(path), FsAccessMode.Write))
        {
            return new ShellCommandResult.PermissionDenied($"mkdir: {target}: permission denied");
        }

        if (!VfsManager.TryCreateDirectory(path, VfsFile.DefaultDirectoryMode))
        {
            return new ShellCommandResult.Error($"mkdir: {target}: cannot create directory (missing parent or read-only)");
        }

        FsPermissions.AssignCallerOwner(path, isDirectory: true);
        return ShellCommandResult.Ok;
    }
}
