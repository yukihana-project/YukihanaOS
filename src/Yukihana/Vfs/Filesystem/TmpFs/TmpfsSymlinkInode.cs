// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsSymlinkInode : TmpfsInode
{
    public TmpfsSymlinkInode()
    {
        Mode = ModeEnum.SymbolicLink | (ModeEnum)0x1ff; // rwxrwxrwx
    }
    public required string Target;

    public override IFileOperations? FileOperations { get; }
}