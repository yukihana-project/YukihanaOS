// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsOpenFile(IVfsOpenFile openFile) : IVfsOpenFile
{
    public TmpfsFileInode Inode => (TmpfsFileInode)openFile.Inode;

    public IFileOperations Operations => new TmpfsFileOperations();

    public long Position { get; set; } = openFile.Position;

    public const int PageSize = 4096;

    public byte this[long index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            long pageIndex = index / PageSize;
            int pageOffset = (int)(index % PageSize);

            if (pageIndex >= Inode.Pages.Count)
            {
                return 0;
            }

            byte[]? page = Inode.Pages[(int)pageIndex];

            return page == null
                ? (byte)0
                : page[pageOffset];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            long pageIndex = index / PageSize;
            int pageOffset = (int)(index % PageSize);

            while (Inode.Pages.Count <= pageIndex)
            {
                Inode.Pages.Add(null);
            }

            byte[]? page = Inode.Pages[(int)pageIndex];

            if (page == null)
            {
                if (value == 0)
                {
                    return;
                }

                page = new byte[PageSize];
                Inode.Pages[(int)pageIndex] = page;
            }

            page[pageOffset] = value;

            if (value == 0 && page.AsSpan().IndexOfAnyExcept((byte)0) < 0)
            {
                Inode.Pages[(int)pageIndex] = null;
            }
        }
    }

    public int Read(Span<byte> destination)
    {
        int total = destination.Length;
        int remaining = total;
        int destOffset = 0;

        while (remaining > 0)
        {
            long pageIndex = Position / PageSize;
            int pageOffset = (int)(Position % PageSize);

            int count = Math.Min(remaining, PageSize - pageOffset);

            if (pageIndex >= Inode.Pages.Count)
            {
                destination.Slice(destOffset, count).Clear();
            }
            else
            {
                byte[]? page = Inode.Pages[(int)pageIndex];

                if (page == null)
                {
                    destination.Slice(destOffset, count).Clear();
                }
                else
                {
                    page.AsSpan(pageOffset, count)
                        .CopyTo(destination.Slice(destOffset, count));
                }
            }

            Position += count;
            destOffset += count;
            remaining -= count;
        }

        return total;
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        int remaining = source.Length;
        int sourceOffset = 0;

        while (remaining > 0)
        {
            long pageIndex = Position / PageSize;
            int pageOffset = (int)(Position % PageSize);

            while (Inode.Pages.Count <= pageIndex)
            {
                Inode.Pages.Add(null);
            }

            int count = Math.Min(remaining, PageSize - pageOffset);

            byte[]? page = Inode.Pages[(int)pageIndex];
            ReadOnlySpan<byte> chunk = source.Slice(sourceOffset, count);

            if (page == null)
            {
                if (chunk.IndexOfAnyExcept((byte)0) >= 0)
                {
                    page = new byte[PageSize];
                    Inode.Pages[(int)pageIndex] = page;
                }
            }

            if (page != null)
            {
                chunk.CopyTo(page.AsSpan(pageOffset, count));

                if (page.AsSpan().IndexOfAnyExcept((byte)0) < 0)
                {
                    Inode.Pages[(int)pageIndex] = null;
                }
            }

            Position += count;
            sourceOffset += count;
            remaining -= count;

            if (Position > Inode.Size)
            {
                Inode.Size = Position;
            }
        }
    }


    IVfsInode IVfsOpenFile.Inode => Inode;
}