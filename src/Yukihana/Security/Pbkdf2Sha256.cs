// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Buffers.Binary;

namespace Yukihana.Security;


// salt = sha256(username + uid + creation time)
public static class Pbkdf2Sha256
{
    private const int HashSize = HmacSha256.HashSize;

    public const int DefaultIterations = 25_000;
    public const int DefaultKeyLength = HmacSha256.HashSize;

    public static byte[] DeriveKey(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        int iterations = DefaultIterations,
        int keyLength = DefaultKeyLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keyLength);

        HmacSha256 hmac = new(password);

        byte[] derivedKey = new byte[keyLength];

        Span<byte> u = stackalloc byte[HashSize];
        Span<byte> t = stackalloc byte[HashSize];

        byte[] saltBlock = new byte[salt.Length + 4];
        salt.CopyTo(saltBlock);

        int blockCount = (keyLength + HashSize - 1) / HashSize;
        int offset = 0;

        for (int block = 1; block <= blockCount; block++)
        {
            BinaryPrimitives.WriteInt32BigEndian(
                saltBlock.AsSpan(salt.Length),
                block);

            // U1
            hmac.Compute(saltBlock, u);
            u.CopyTo(t);

            // U2..Uc
            for (int i = 1; i < iterations; i++)
            {
                hmac.Compute(u, u);

                for (int j = 0; j < HashSize; j++)
                {
                    t[j] ^= u[j];
                }
            }

            int length = Math.Min(HashSize, keyLength - offset);
            t[..length].CopyTo(derivedKey.AsSpan(offset));

            offset += length;
        }

        return derivedKey;
    }
}
