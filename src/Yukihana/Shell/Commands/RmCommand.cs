// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class RmCommand : IShellCommand
{
    public string Name => "rm";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Remove a file";
    public string Usage => "rm <file>";
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
            return new ShellCommandResult.Error($"rm: {target}: no such file");
        }

        if (stat.IsDirectory)
        {
            return new ShellCommandResult.Error($"rm: {target}: is a directory, use rmdir");
        }

        if (!FsPermissions.Allows(ShellSession.Parent(path), FsAccessMode.Write))
        {
            return new ShellCommandResult.PermissionDenied($"rm: {target}: permission denied");
        }

        return VfsManager.TryUnlink(path)
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error($"rm: {target}: cannot remove file");
    }
}
