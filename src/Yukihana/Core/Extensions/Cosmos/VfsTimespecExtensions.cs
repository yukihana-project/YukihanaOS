// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;

namespace Yukihana.Core.Extensions.Cosmos;

public static class VfsTimespecExtensions
{
    extension(VfsTimespec)
    {
        public static VfsTimespec Now()
        {
            return new VfsTimespec(DateTimeOffset.Now.ToUnixTimeSeconds(), 0);
        }
    }
}
