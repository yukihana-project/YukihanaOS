// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;


// salt = sha256(username + uid + creation time)
public static class Pbkdf2Sha256
{
    private const int HashSize = 32;

    public static byte[] DeriveKey(
        byte[] password,
        byte[] salt,
        int iterations,
        int keyLength)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(salt);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keyLength);

        int blockCount = (keyLength + HashSize - 1) / HashSize;

        byte[] derivedKey = new byte[keyLength];
        byte[] block = new byte[HashSize];

        int offset = 0;

        for (int i = 1; i <= blockCount; i++)
        {
            ComputeBlock(password, salt, iterations, i, block);

            int length = Math.Min(HashSize, keyLength - offset);
            Buffer.BlockCopy(block, 0, derivedKey, offset, length);

            offset += length;
        }

        return derivedKey;
    }

    private static void ComputeBlock(
        byte[] password,
        byte[] salt,
        int iterations,
        int blockIndex,
        byte[] output)
    {
        byte[] saltBlock = new byte[salt.Length + 4];

        Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length);

        // INT(blockIndex) in big-endian
        saltBlock[salt.Length + 0] = (byte)(blockIndex >> 24);
        saltBlock[salt.Length + 1] = (byte)(blockIndex >> 16);
        saltBlock[salt.Length + 2] = (byte)(blockIndex >> 8);
        saltBlock[salt.Length + 3] = (byte)blockIndex;

        byte[] u = HmacSha256.Compute(password, saltBlock);

        Buffer.BlockCopy(u, 0, output, 0, HashSize);

        for (int i = 1; i < iterations; i++)
        {
            u = HmacSha256.Compute(password, u);

            for (int j = 0; j < HashSize; j++)
            {
                output[j] ^= u[j];
            }
        }
    }
}
