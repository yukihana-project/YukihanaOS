// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

[Flags]
public enum Capability : ulong
{
    None = 0,

    MountFilesystem = 1 << 0,
    Shutdown = 1 << 1,
    ChangeMode = 1 << 2,
    ChangePermissions = 1 << 3,

    All = ulong.MaxValue
}
