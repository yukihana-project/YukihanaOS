// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public sealed class UserAccount
{
    public required User User { get; init; }
    public required PasswordHash Password { get; set; }
    public bool Locked { get; set; } = false;
    public DateTimeOffset? PasswordExpires { get; set; } = DateTimeOffset.MaxValue;
}
