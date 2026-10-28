// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class MvCommand : IShellCommand
{
    public string Name => "mv";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Move or rename a file or directory";
    public string Usage => "mv <source> <target>";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 2)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string source = context.Session.ResolvePath(context.Arguments[0]);
        string target = context.Session.ResolvePath(context.Arguments[1]);

        if (!VfsManager.TryStat(source, out VfsStat _))
        {
            return new ShellCommandResult.Error($"mv: {context.Arguments[0]}: no such file or directory");
        }

        // Moving into an existing directory keeps the source name
        if (VfsManager.TryStat(target, out VfsStat targetStat) && targetStat.IsDirectory)
        {
            target = ShellSession.Child(target, ShellSession.BaseName(source));
        }

        if (source == target)
        {
            return ShellCommandResult.Ok;
        }

        // Moving removes the entry from one directory and adds it to another
        if (!FsPermissions.Allows(ShellSession.Parent(source), FsAccessMode.Write) ||
            !FsPermissions.Allows(ShellSession.Parent(target), FsAccessMode.Write))
        {
            return new ShellCommandResult.PermissionDenied($"mv: '{context.Arguments[0]}' to '{context.Arguments[1]}': permission denied");
        }

        return VfsManager.TryRename(source, target)
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error($"mv: cannot move '{context.Arguments[0]}' to '{context.Arguments[1]}'");
    }
}
