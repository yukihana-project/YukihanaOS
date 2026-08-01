// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public sealed class UserProfile
{
    public required UserId User { get; init; }
    public required string HomeDirectory { get; init; }
    public required string Shell { get; init; }
    public string? FullName { get; init; }

    public static readonly UserProfile Root = new()
    {
        User = UserId.Root,
        HomeDirectory = "/root",
        Shell = "ksh",
        FullName = "root"
    };
}
