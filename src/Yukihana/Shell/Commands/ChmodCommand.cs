// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class ChmodCommand : IShellCommand
{
    public string Name => "chmod";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Change file permission bits (octal)";
    public string Usage => "chmod <octal> <path>";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 2 || !TryParseOctal(context.Arguments[0], out uint permissions))
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string target = context.Arguments[1];
        string path = context.Session.ResolvePath(target);

        if (!VfsManager.TryStat(path, out VfsStat stat))
        {
            return new ShellCommandResult.Error($"chmod: {target}: no such file or directory");
        }

        bool isOwner = stat.Uid == Kernel.SecurityManager.Current.EffectiveUser.Value;

        if (!isOwner && !PermissionManager.CanChmod())
        {
            return new ShellCommandResult.PermissionDenied($"chmod: {target}: permission denied");
        }

        stat.Mode = (stat.Mode & VfsMode.FileTypeMask) | (VfsMode)permissions;

        bool applied = false;

        if (stat.IsDirectory)
        {
            if (VfsManager.TryOpenDirectory(path, out var dir))
            {
                using IVfsDirectoryHandle handle = dir!;
                applied = handle.TrySetAttr(SetAttrFlags.Mode, in stat);
            }
        }
        else if (VfsManager.TryOpenFile(path, out var file))
        {
            using IVfsFileHandle handle = file!;
            applied = handle.TrySetAttr(SetAttrFlags.Mode, in stat);
        }

        // A filesystem can accept SetAttr and still keep the old mode, so the result is read back
        if (!applied ||
            !VfsManager.TryStat(path, out VfsStat after) ||
            (after.Mode & VfsMode.PermissionMask) != (stat.Mode & VfsMode.PermissionMask))
        {
            return new ShellCommandResult.Error($"chmod: {target}: filesystem did not apply the mode ({FsPermissions.FilesystemOf(path)})");
        }

        return ShellCommandResult.Ok;
    }

    // Three digits at most, so the result never exceeds VfsMode.PermissionMask (0o777)
    private static bool TryParseOctal(string text, out uint value)
    {
        value = 0;

        if (text.Length is 0 or > 3)
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c is < '0' or > '7')
            {
                return false;
            }

            value = value * 8 + (uint)(c - '0');
        }

        return true;
    }
}
