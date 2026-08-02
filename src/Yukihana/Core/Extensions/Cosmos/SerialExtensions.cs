// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.Core.IO;

namespace Yukihana.Core.Extensions.Cosmos;

public static class SerialExtensions
{
    extension(Serial)
    {
        public static unsafe void WriteReadOnlyString(ReadOnlySpan<char> span)
        {
            fixed (char* ptr = span)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    Serial.ComWrite((byte)ptr[i]);
                }
            }
        }
    }
}
