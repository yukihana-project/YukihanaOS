// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class MountCommand : IShellCommand
{
    public string Name => "mount";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Mount a filesystem";
    public string Usage => "mount <fstype> <source> <target>";
    public Capability RequiredCapabilities => Capability.MountFilesystem;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 3)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string fsType = context.Arguments[0];
        string source = context.Arguments[1];
        string target = context.Session.ResolvePath(context.Arguments[2]);

        // shortcut: source is passed as a raw string, so fstab-style UUID lookup is not supported, upgrade by reusing VfsConfigManager.TryMount(config)
        if (!VfsManager.TryMount(fsType, source, MountFlags.None, target, out _))
        {
            return new ShellCommandResult.Error($"mount: {source}: failed to mount {fsType} at {target}");
        }

        return ShellCommandResult.Ok;
    }
}
