// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsDirectoryInode : TmpfsInode
{
    public TmpfsDirectoryInode()
    {
        Mode = ModeEnum.Directory | (ModeEnum)0x1ed; // rwxr-xr-x
    }
    public readonly Dictionary<string, TmpfsInode> Children = new(StringComparer.Ordinal);

}
