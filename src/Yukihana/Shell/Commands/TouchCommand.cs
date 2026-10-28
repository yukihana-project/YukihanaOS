// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;
using Yukihana.Vfs;

namespace Yukihana.Shell.Commands;

public sealed class TouchCommand : IShellCommand
{
    public string Name => "touch";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Create an empty file if it does not exist";
    public string Usage => "touch <file>";
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

        // shortcut: an existing file is left alone, timestamps are not updated
        if (VfsManager.TryStat(path, out VfsStat _))
        {
            return ShellCommandResult.Ok;
        }

        if (!FsPermissions.Allows(ShellSession.Parent(path), FsAccessMode.Write))
        {
            return new ShellCommandResult.PermissionDenied($"touch: {target}: permission denied");
        }

        if (!VfsManager.TryCreateFile(path, VfsFile.DefaultFileMode))
        {
            return new ShellCommandResult.Error($"touch: {target}: cannot create file");
        }

        FsPermissions.AssignCallerOwner(path, isDirectory: false);
        return ShellCommandResult.Ok;
    }
}
