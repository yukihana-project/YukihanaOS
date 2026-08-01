// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;

namespace Yukihana.Security;

public static class ProfileManager
{
    private static readonly ConcurrentDictionary<UserId, UserProfile> s_userProfiles = new()
    {
        [UserId.Root] = UserProfile.Root
    };

    public static void AddProfile(UserId uid, string fullName, string? homeDirectory = null, string? defaultShell = null)
    {
        User? user = UserManager.GetUser(uid);

        if (user is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            homeDirectory = user.Name.Replace(" ", "").ToLower();
        }

        if (string.IsNullOrWhiteSpace(defaultShell))
        {
            defaultShell = "ksh"; // kernel shell
        }

        s_userProfiles.TryAdd(uid, new UserProfile { User = uid, HomeDirectory = homeDirectory, Shell = defaultShell });
    }

    public static void RemoveProfile(User user)
    {
        s_userProfiles.TryRemove(user.Id, out _);
    }

    public static UserProfile? GetProfile(UserId uid)
    {
        return s_userProfiles.TryGetValue(uid, out UserProfile? profile) ? profile : null;
    }
}
