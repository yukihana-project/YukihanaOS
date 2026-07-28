// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public static class SecurityContextFactory
{
    public static SecurityContext CreateKernelContext() => new()
    {
        RealUser = UserId.Root,
        EffectiveUser = UserId.Root,
        SavedUser = UserId.Root,

        RealGroup = GroupId.Root,
        EffectiveGroup = GroupId.Root,
        SavedGroup = GroupId.Root,

        SupplementaryGroups = [],

        Capabilities = CapabilitySet.Root,

        IsKernel = true
    };

    public static SecurityContext CreateRootContext() => new()
    {
        RealUser = UserId.Root,
        EffectiveUser = UserId.Root,
        SavedUser = UserId.Root,

        RealGroup = GroupId.Root,
        EffectiveGroup = GroupId.Root,
        SavedGroup = GroupId.Root,

        SupplementaryGroups = [GroupId.Root],

        Capabilities = CapabilitySet.Root,

        IsKernel = false
    };

    public static SecurityContext CreateUserContext(User user) => new()
    {
        RealUser = user.Id,
        EffectiveUser = user.Id,
        SavedUser = user.Id,

        RealGroup = user.PrimaryGroup,
        EffectiveGroup = user.PrimaryGroup,
        SavedGroup = user.PrimaryGroup,

        SupplementaryGroups = user.SupplementaryGroups,

        Capabilities = user.DefaultCapabilities,

        IsKernel = false
    };
}
