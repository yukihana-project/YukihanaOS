// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using acryptohashnet;

namespace Yukihana.Security;

public static class HmacSha256
{
    private const int BlockSize = 64;
    private const int HashSize = 32;

    public static byte[] Compute(byte[] key, byte[] message)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(message);

        SHA256 sha = new();

        if (key.Length > BlockSize)
        {
            key = sha.ComputeHash(key);
        }

        byte[] keyBlock = new byte[BlockSize];
        Buffer.BlockCopy(key, 0, keyBlock, 0, key.Length);

        byte[] ipad = new byte[BlockSize];
        byte[] opad = new byte[BlockSize];

        for (int i = 0; i < BlockSize; i++)
        {
            ipad[i] = (byte)(keyBlock[i] ^ 0x36);
            opad[i] = (byte)(keyBlock[i] ^ 0x5c);
        }

        byte[] inner = new byte[BlockSize + message.Length];
        Buffer.BlockCopy(ipad, 0, inner, 0, BlockSize);
        Buffer.BlockCopy(message, 0, inner, BlockSize, message.Length);

        byte[] innerHash = sha.ComputeHash(inner);

        byte[] outer = new byte[BlockSize + HashSize];
        Buffer.BlockCopy(opad, 0, outer, 0, BlockSize);
        Buffer.BlockCopy(innerHash, 0, outer, BlockSize, HashSize);

        return sha.ComputeHash(outer);
    }
}
