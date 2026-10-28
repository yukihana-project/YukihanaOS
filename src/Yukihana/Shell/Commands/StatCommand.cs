// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class StatCommand : IShellCommand
{
    public string Name => "stat";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Show type, owner and permission bits of a path";
    public string Usage => "stat <path>";
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
            return new ShellCommandResult.Error($"stat: {target}: no such file or directory");
        }

        string type = stat.IsDirectory ? "directory"
            : stat.IsSymbolicLink ? "symlink"
            : stat.IsRegularFile ? "regular file"
            : "other";

        string checks = !FsPermissions.HasUsableMode(stat)
            ? "capability fallback (filesystem reported no usable mode)"
            : !FsPermissions.StoresPermissions(path)
                ? "capability fallback (filesystem does not store modes)"
                : "filesystem mode";

        int permissions = (int)(stat.Mode & VfsMode.PermissionMask);
        string octal = $"{(permissions >> 6) & 7}{(permissions >> 3) & 7}{permissions & 7}";

        context.Out.WriteLine($"  path:   {path}");
        context.Out.WriteLine($"  type:   {type}");
        context.Out.WriteLine($"  mode:   {octal} (raw 0x{(ulong)stat.Mode:X})");
        context.Out.WriteLine($"  fs:     {FsPermissions.FilesystemOf(path)}");
        context.Out.WriteLine($"  owner:  uid {stat.Uid}, gid {stat.Gid}");
        context.Out.WriteLine($"  size:   {stat.Size}");
        context.Out.WriteLine($"  checks: {checks}");

        return ShellCommandResult.Ok;
    }
}
