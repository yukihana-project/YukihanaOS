// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;
using Cosmos.Kernel.System.FileSystem;

namespace Yukihana.Vfs;

// Writes whole files through the VFS handles, System.IO has no write backing on Cosmos
public static class VfsFile
{
    // Permission bits are hex, C# has no octal literals: 0o644 = 0x1A4, 0o755 = 0x1ED
    public const VfsMode DefaultFileMode = VfsMode.RegularFile | (VfsMode)0x1A4;
    public const VfsMode PrivateFileMode = VfsMode.RegularFile | VfsMode.OwnerRead | VfsMode.OwnerWrite;
    public const VfsMode DefaultDirectoryMode = VfsMode.Directory | (VfsMode)0x1ED;

    public static bool Write(string path, ReadOnlySpan<byte> data, VfsMode mode = DefaultFileMode)
    {
        // shortcut: unlink and recreate, not atomic, a power cut during save loses the file
        VfsManager.TryUnlink(path);

        if (!VfsManager.TryCreateFile(path, mode))
        {
            return false;
        }

        if (!VfsManager.TryOpenFile(path, out IVfsFileHandle? file))
        {
            return false;
        }

        using IVfsFileHandle handle = file!;
        return handle.Write(data) == data.Length && handle.TryFlush();
    }

    public static bool Write(string path, string text, VfsMode mode = DefaultFileMode)
    {
        return Write(path, Encoding.UTF8.GetBytes(text), mode);
    }
}
