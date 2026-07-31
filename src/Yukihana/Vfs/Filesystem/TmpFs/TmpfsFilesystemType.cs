// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Diagnostics.CodeAnalysis;
using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Core.Extensions.Cosmos;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsFilesystemType(ulong sizeInBytes) : IVfsFilesystemType
{
    public bool TryDestroy(ReadOnlySpan<char> source) => true;
    public bool TryFormat(ReadOnlySpan<char> source, [NotNullWhen(true)] IVfsFormatOptions? options) => true;
    public bool TryMount(ReadOnlySpan<char> source, MountFlags flags, [NotNullWhen(true)] out IVfsSuperblock? superblock)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual<ulong>(sizeInBytes % 4096, 0, "sizeInBytes");

        var now = VfsTimespec.Now();

        var rootDir = new TmpfsDirectoryInode()
        {
            Name = "/",
            Ctime = now,
            Atime = now,
            Mtime = now,
            LinkCount = 2,
        };

        var super = new TmpfsSuperblock(sizeInBytes, rootDir);

        rootDir.Superblock = super;

        superblock = super;
        return true;
    }
}