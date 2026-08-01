// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Debug;

namespace Yukihana.Security;


// salt = sha256(username + uid + creation time)
public static class Pbkdf2Sha256
{
    private const int HashSize = 32;

    public const int Interations = 100_000;
    public const int KeyLength = 32;

    public static bool DoLog = false;

    public static byte[] DeriveKey(
        byte[] password,
        byte[] salt,
        int iterations,
        int keyLength)
    {
        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Driving key");
        }

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

            if (DoLog)
            {
                Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Coping ");
            }

            Buffer.BlockCopy(block, 0, derivedKey, offset, length);
            
            offset += length;
            
            if (DoLog)
            {
                Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Moving on");
            }
        }

        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Got key");
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
        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Computing block");
        }

        byte[] saltBlock = new byte[salt.Length + 4];

        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Block copy 1");
        }

        Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length);

        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> big-endian");
        }
    
        // INT(blockIndex) in big-endian
        saltBlock[salt.Length + 0] = (byte)(blockIndex >> 24);
        saltBlock[salt.Length + 1] = (byte)(blockIndex >> 16);
        saltBlock[salt.Length + 2] = (byte)(blockIndex >> 8);
        saltBlock[salt.Length + 3] = (byte)blockIndex;
        
        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> HmacSha256");
        }
    
        byte[] u = HmacSha256.Compute(password, saltBlock);
        
        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> Block copy 2");
        }

        Buffer.BlockCopy(u, 0, output, 0, HashSize);
        
        if (DoLog)
        {
            Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> iterrating");
        }

        for (int i = 1; i < iterations; i++)
        {
            if (DoLog)
            {
                Logger.GlobalLogger.Trace($"Pbkdf2Sha256 -> iterration [{i}]");
            }

            u = HmacSha256.Compute(password, u);

            if (DoLog)
            {
                Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> got hmac");
            }

            if (DoLog)
            {
                Logger.GlobalLogger.Trace("Pbkdf2Sha256 -> hashing");
            }

            for (int j = 0; j < HashSize; j++)
            {
                output[j] ^= u[j];
            }
        }
    }
}
