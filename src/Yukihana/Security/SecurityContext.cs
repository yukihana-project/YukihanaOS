// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Immutable;

namespace Yukihana.Security;

public sealed record SecurityContext
{
    public required UserId RealUser { get; init; }

    public required UserId EffectiveUser { get; init; }

    public required UserId SavedUser { get; init; }

    public required GroupId RealGroup { get; init; }

    public required GroupId EffectiveGroup { get; init; }

    public required GroupId SavedGroup { get; init; }

    public required ImmutableHashSet<GroupId> SupplementaryGroups { get; init; }

    public required CapabilitySet Capabilities { get; init; }

    public static SecurityContext Root { get; } = new()
    {
        RealUser = UserId.Root,
        EffectiveUser = UserId.Root,
        SavedUser = UserId.Root,
        RealGroup = GroupId.Root,
        EffectiveGroup = GroupId.Root,
        SavedGroup = GroupId.Root,
        SupplementaryGroups = [GroupId.Root],
        Capabilities = CapabilitySet.Root
    };

    public SecurityContext WithEffectiveIdentity(
        UserId user,
        GroupId group)
        => this with
        {
            EffectiveUser = user,
            EffectiveGroup = group
        };

    public SecurityContext Elevate()
    {
        if (IsRoot)
        {
            return this;
        }
        else
        {
            return this with
            {
                SavedUser = EffectiveUser,
                SavedGroup = EffectiveGroup,
                EffectiveUser = UserId.Root,
                EffectiveGroup = GroupId.Root
            };
        }
    }

    public SecurityContext Restore()
        => this with
        {
            EffectiveUser = SavedUser,
            EffectiveGroup = SavedGroup
        };

    public bool IsRoot
        => EffectiveUser == UserId.Root;

    public bool IsMemberOf(GroupId group)
        => EffectiveGroup == group || SupplementaryGroups.Contains(group);

    public bool HasCapability(Capability capability)
        => Capabilities.Contains(capability);

    public bool HasAnyCapability(Capability capabilities)
        => Capabilities.Intersects(capabilities);

    public bool HasAllCapabilities(Capability capabilities)
        => Capabilities.ContainsAll(capabilities);
}
