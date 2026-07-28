// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Immutable;

namespace Yukihana.Security;

public sealed class User
{
    public required UserId Id { get; init; }

    public required string Name { get; init; }

    public required GroupId PrimaryGroup { get; init; }

    public required ImmutableHashSet<GroupId> SupplementaryGroups { get; init; }

    public required CapabilitySet DefaultCapabilities { get; init; }

    public bool Enabled { get; init; } = true;
}
