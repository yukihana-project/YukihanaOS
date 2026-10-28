// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class LsCommand : IShellCommand
{
    public string Name => "ls";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "List directory entries";
    public string Usage => "ls [path]";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        string target = context.Arguments.Count > 0 ? context.Arguments[0] : ".";
        string path = context.Session.ResolvePath(target);

        if (!FsPermissions.Allows(path, FsAccessMode.Read))
        {
            return new ShellCommandResult.PermissionDenied($"ls: {target}: permission denied");
        }

        if (!VfsManager.TryOpenDirectory(path, out var dir))
        {
            return new ShellCommandResult.Error($"ls: {target}: no such directory");
        }

        if (!dir!.TryReadDir(out var entries))
        {
            return new ShellCommandResult.Error($"ls: {target}: cannot read directory");
        }

        foreach (IVfsInode entry in entries!)
        {
            bool isDirectory = entry.InodeOperations.GetAttr(entry, out VfsStat stat) && stat.IsDirectory;
            context.Out.WriteLine(isDirectory ? $"{entry.Name}/" : entry.Name);
        }

        return ShellCommandResult.Ok;
    }
}
