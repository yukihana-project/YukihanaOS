// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using acryptohashnet;

public sealed class HmacSha256
{
    private const int BlockSize = 64;
    public const int HashSize = 32;

    private readonly byte[] _ipad = new byte[BlockSize];
    private readonly byte[] _opad = new byte[BlockSize];
    private readonly SHA256 _sha = new();

    public HmacSha256(ReadOnlySpan<byte> key)
    {
        Span<byte> keyBlock = stackalloc byte[BlockSize];

        if (key.Length > BlockSize)
        {
            byte[] hashed = _sha.ComputeHash([.. key]);
            hashed.CopyTo(keyBlock);
        }
        else
        {
            key.CopyTo(keyBlock);
        }

        for (int i = 0; i < BlockSize; i++)
        {
            _ipad[i] = (byte)(keyBlock[i] ^ 0x36);
            _opad[i] = (byte)(keyBlock[i] ^ 0x5C);
        }
    }

    public byte[] Compute(ReadOnlySpan<byte> message)
    {
        byte[] result = new byte[HashSize];
        Compute(message, result);
        return result;
    }

    public void Compute(ReadOnlySpan<byte> message, Span<byte> destination)
    {
        if (destination.Length < HashSize)
        {
            throw new ArgumentException(null, nameof(destination));
        }

        byte[] inner = new byte[BlockSize + message.Length];

        Buffer.BlockCopy(_ipad, 0, inner, 0, BlockSize);
        message.CopyTo(inner.AsSpan(BlockSize));

        byte[] innerHash = _sha.ComputeHash(inner);

        byte[] outer = new byte[BlockSize + HashSize];

        Buffer.BlockCopy(_opad, 0, outer, 0, BlockSize);
        innerHash.CopyTo(outer.AsSpan(BlockSize));

        byte[] hash = _sha.ComputeHash(outer);

        hash.CopyTo(destination);
    }
}
