// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public sealed class UserAccount
{
    public required User User { get; init; }
    // TODO: passwords
    public bool Locked { get; set; }
    public DateTimeOffset? PasswordExpires { get; set; }
}
