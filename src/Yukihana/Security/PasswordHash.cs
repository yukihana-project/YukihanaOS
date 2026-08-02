// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public enum PasswordHashAlgorithm
{
    Pbkdf2Sha256
}

public sealed record PasswordHash(byte[] Salt, byte[] Hash, PasswordHashAlgorithm UsedAlgorithm);
