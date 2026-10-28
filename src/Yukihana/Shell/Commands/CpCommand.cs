// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;
using Yukihana.Vfs;

namespace Yukihana.Shell.Commands;

public sealed class CpCommand : IShellCommand
{
    public string Name => "cp";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Copy a file";
    public string Usage => "cp <source> <target>";
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

        if (!VfsManager.TryStat(source, out VfsStat sourceStat))
        {
            return new ShellCommandResult.Error($"cp: {context.Arguments[0]}: no such file");
        }

        if (sourceStat.IsDirectory)
        {
            return new ShellCommandResult.Error($"cp: {context.Arguments[0]}: is a directory, copying directories is not supported");
        }

        if (!FsPermissions.Allows(source, FsAccessMode.Read))
        {
            return new ShellCommandResult.PermissionDenied($"cp: {context.Arguments[0]}: permission denied");
        }

        // Copying into an existing directory keeps the source name
        if (VfsManager.TryStat(target, out VfsStat targetStat) && targetStat.IsDirectory)
        {
            target = ShellSession.Child(target, ShellSession.BaseName(source));
        }

        // An existing target is overwritten in place, a new one is created in its parent
        bool exists = VfsManager.TryStat(target, out VfsStat existing);
        string writable = exists ? target : ShellSession.Parent(target);

        if (!FsPermissions.Allows(writable, FsAccessMode.Write))
        {
            return new ShellCommandResult.PermissionDenied($"cp: {context.Arguments[1]}: permission denied");
        }

        byte[] data = File.ReadAllBytes(source);

        // The VFS recreates the file on write, so the old mode and owner are restored afterwards
        VfsMode mode = exists
            ? (existing.Mode & VfsMode.PermissionMask) | VfsMode.RegularFile
            : VfsFile.DefaultFileMode;

        if (!VfsFile.Write(target, data, mode))
        {
            return new ShellCommandResult.Error($"cp: cannot write '{context.Arguments[1]}'");
        }

        if (exists)
        {
            FsPermissions.AssignOwner(target, isDirectory: false, existing.Uid, existing.Gid);
        }
        else
        {
            FsPermissions.AssignCallerOwner(target, isDirectory: false);
        }

        return ShellCommandResult.Ok;
    }
}
