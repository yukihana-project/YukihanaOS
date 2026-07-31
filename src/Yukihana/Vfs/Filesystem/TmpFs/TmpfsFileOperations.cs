// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsFileOperations : IFileOperations
{
    public bool Fsync(IVfsOpenFile openFile) => true;
    public void Release(IVfsOpenFile openFile) { }
    public long Read(IVfsOpenFile openFile, Span<byte> buffer)
    {
        var file = new TmpfsOpenFile(openFile);

        return file.Read(buffer);
    }
    public bool Seek(IVfsOpenFile openFile, long offset, SeekWhence whence, out long newPosition)
    {
        newPosition = openFile.Position;

        switch (whence)
        {
            case SeekWhence.Set:
                newPosition = offset;
                break;
            case SeekWhence.Cur:
            case SeekWhence.End:
                newPosition += offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(whence), whence, null);
        }

        return true;
    }
    public long Write(IVfsOpenFile openFile, ReadOnlySpan<byte> buffer)
    {
        var file = new TmpfsOpenFile(openFile);

        file.Write(buffer);

        return buffer.Length;
    }
}
