// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsSuperblockOperations : ISuperblockOperations
{
    public void Drop(IVfsSuperblock superblock) { }
    public bool StatFs(IVfsSuperblock superblock, out VfsStatFs statFs)
    {
        if (superblock is not TmpfsSuperblock tmpfsSuper)
        {
            statFs = new VfsStatFs();
            return false;
        }

        statFs = new VfsStatFs()
        {
            Type = 0x746d70667e, // "tmpfs" ASCII
            BlockSize = (ulong)tmpfsSuper.BlockSize,
            Blocks = tmpfsSuper.MaxBytes / (ulong)tmpfsSuper.BlockSize,
            Bfree = (tmpfsSuper.MaxBytes - tmpfsSuper.UsedBytes) / (ulong)tmpfsSuper.BlockSize,
            Bavail = (tmpfsSuper.MaxBytes - tmpfsSuper.UsedBytes) / (ulong)tmpfsSuper.BlockSize,
            Files = ulong.MaxValue,
            Ffree = ulong.MaxValue - tmpfsSuper.UsedInodes,
            NameMax = tmpfsSuper.MaxNameLength,
            Frsize = 4096
        };
        return true;
    }
    public bool Sync(IVfsSuperblock superblock) => true;
}
