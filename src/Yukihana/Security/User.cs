// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public sealed class User
{
    public required UserId Id { get; init; }

    public required string Name { get; init; }

    public required GroupId PrimaryGroup { get; init; }

    public required IReadOnlyList<GroupId> SupplementaryGroups { get; init; }
}
