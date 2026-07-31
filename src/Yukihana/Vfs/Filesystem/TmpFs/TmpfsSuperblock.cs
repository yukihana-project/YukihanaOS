// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsSuperblock(ulong maxBytes, TmpfsDirectoryInode root) : IVfsSuperblock
{
    public IVfsInode Root => root;

    public ISuperblockOperations SuperOperations => new TmpfsSuperblockOperations();

    public long BlockSize => 4096;

    public ulong MaxNameLength => 255;

    public ulong MaxBytes { get; } = maxBytes;
    public ulong UsedBytes { get; set; } = 0;
    public ulong UsedInodes { get; set; } = 0;
}
