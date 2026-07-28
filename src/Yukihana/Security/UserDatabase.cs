// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public sealed class UserDatabase
{
    public IList<User> Users { get; } = [];

    public IList<Group> Groups { get; } = [];
}
