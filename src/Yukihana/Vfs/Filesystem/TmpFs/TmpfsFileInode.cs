// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsFileInode : TmpfsInode
{
    public TmpfsFileInode()
    {
        Mode = ModeEnum.RegularFile | (ModeEnum)0x1b4; // rw-rw-r--
    }
    public List<byte[]?> Pages = [];
    
    public long AllocatedSize =>
        Pages.Count(p => p != null) * 4096L;
    
    public long Capacity =>
        Pages.Count * 4096L;

    public override IFileOperations? FileOperations { get; }
}