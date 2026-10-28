// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class UmountCommand : IShellCommand
{
    public string Name => "umount";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Unmount a filesystem";
    public string Usage => "umount <target>";
    public Capability RequiredCapabilities => Capability.MountFilesystem;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 1)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string target = context.Session.ResolvePath(context.Arguments[0]);

        if (!VfsManager.TryUnmount(target))
        {
            return new ShellCommandResult.Error($"umount: {target}: not mounted");
        }

        return ShellCommandResult.Ok;
    }
}
