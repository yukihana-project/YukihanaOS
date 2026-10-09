// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;

namespace Yukihana.Vfs.Filesystem.InitFs;

internal sealed class InitfsSuperblockOperations : ISuperblockOperations
{
    public bool Sync(IVfsSuperblock superblock)
    {
        // No sync needed for read-only filesystem
        return true;
    }

    public bool StatFs(IVfsSuperblock superblock, out VfsStatFs statFs)
    {
        // For a read-only initramfs, we report 0 available space since it's not a real disk
        statFs = new VfsStatFs
        {
            Type = 0x696e6974, // "init" ASCII
            BlockSize = 512,
            Blocks = 0,
            FreeBlocks = 0,
            AvailableBlocks = 0,
            FreeInodes = 0,
            MaxNameLength = 255,
            FragmentSize = 512
        };
        return true;
    }

    public void Drop(IVfsSuperblock superblock)
    {
        // No cleanup needed for this filesystem
    }
}
