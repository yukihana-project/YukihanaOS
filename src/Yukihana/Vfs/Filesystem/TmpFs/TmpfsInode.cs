// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public abstract class TmpfsInode : IVfsInode
{
    private static ulong s_nextInodeId;
    public ulong InodeNumber = ++s_nextInodeId;
    public required string Name;
    public ModeEnum Mode;
    public TmpfsDirectoryInode? Parent;

    public long Size { get; set; }

    public uint Uid = 0;
    public uint Gid = 0;

    public VfsTimespec Atime;
    public VfsTimespec Mtime;
    public VfsTimespec Ctime;

    public uint LinkCount = 1;

    public IFileOperations? FileOperations => new TmpfsFileOperations();

    public IInodeOperations InodeOperations => new TmpfsInodeOperations();

    public TmpfsSuperblock? Superblock;

    string IVfsInode.Name => Name;
}
