// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Yukihana.Vfs;

namespace Yukihana.Security;

public enum FsAccessMode
{
    Read,
    Write,
    Execute
}

// Checks VFS mode bits and owner. When the VFS gives no usable mode, writes fall back to the capability check
// and reads and execute are allowed, as they were before this check existed
public static class FsPermissions
{
    // Filesystems whose drivers report a mode they do not store. Their chmod is ignored, so their modes are not trusted.
    // shortcut: FAT is the only one, drop it from this list once ext2 is mounted as root and stat/chmod behave on it
    private static readonly string[] s_modeless = ["fat"];

    // A mode without file-type bits means the filesystem reported no usable mode
    public static bool HasUsableMode(VfsStat stat) => (stat.Mode & VfsMode.FileTypeMask) != 0;

    public static string FilesystemOf(string path)
    {
        VfsMount? found = null;

        foreach (VfsMount mount in VfsManager.Mounts)
        {
            string point = mount.MountPoint.TrimEnd('/');

            if ((path == point || path.StartsWith(point + "/")) &&
                (found is null || point.Length > found.MountPoint.TrimEnd('/').Length))
            {
                found = mount;
            }
        }

        return found?.Name ?? "unknown";
    }

    public static bool StoresPermissions(string path)
    {
        string filesystem = FilesystemOf(path);
        return !s_modeless.Contains(filesystem, StringComparer.OrdinalIgnoreCase);
    }

    public static bool Allows(string path, FsAccessMode access)
    {
        SecurityContext context = Kernel.SecurityManager.Current;

        if (!VfsManager.TryStat(path, out VfsStat stat) || !HasUsableMode(stat) || !StoresPermissions(path))
        {
            return access != FsAccessMode.Write || context.HasCapability(Capability.WriteFilesystem);
        }

        if (context.IsRoot)
        {
            return true;
        }

        (VfsMode owner, VfsMode group, VfsMode other) = access switch
        {
            FsAccessMode.Read => (VfsMode.OwnerRead, VfsMode.GroupRead, VfsMode.OtherRead),
            FsAccessMode.Write => (VfsMode.OwnerWrite, VfsMode.GroupWrite, VfsMode.OtherWrite),
            _ => (VfsMode.OwnerExecute, VfsMode.GroupExecute, VfsMode.OtherExecute)
        };

        if (stat.Uid == context.EffectiveUser.Value)
        {
            return (stat.Mode & owner) != 0;
        }

        if (context.IsMemberOf(new GroupId(stat.Gid)))
        {
            return (stat.Mode & group) != 0;
        }

        return (stat.Mode & other) != 0;
    }

    // A node that was just created or recreated belongs to the caller
    public static void AssignCallerOwner(string path, bool isDirectory)
    {
        SecurityContext context = Kernel.SecurityManager.Current;
        AssignOwner(path, isDirectory, context.EffectiveUser.Value, context.EffectiveGroup.Value);
    }

    // Best effort: if the filesystem does not support SetAttr for owners, the VFS default stays
    public static void AssignOwner(string path, bool isDirectory, uint uid, uint gid)
    {
        if (!StoresPermissions(path))
        {
            return;
        }

        VfsStat owner = default;
        owner.Uid = uid;
        owner.Gid = gid;

        if (isDirectory)
        {
            if (VfsManager.TryOpenDirectory(path, out IVfsDirectoryHandle? dir))
            {
                using IVfsDirectoryHandle handle = dir!;
                handle.TrySetAttr(SetAttrFlags.Uid | SetAttrFlags.Gid, in owner);
            }
        }
        else if (VfsManager.TryOpenFile(path, out IVfsFileHandle? file))
        {
            using IVfsFileHandle handle = file!;
            handle.TrySetAttr(SetAttrFlags.Uid | SetAttrFlags.Gid, in owner);
        }
    }
}
