// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public static class SecurityContextFactory
{
    public static SecurityContext CreateUserContext(User user) => new()
    {
        RealUser = user.Id,
        EffectiveUser = user.Id,
        SavedUser = user.Id,

        RealGroup = user.PrimaryGroup,
        EffectiveGroup = user.PrimaryGroup,
        SavedGroup = user.PrimaryGroup,

        SupplementaryGroups = user.SupplementaryGroups,

        Capabilities = user.DefaultCapabilities
    };
}
