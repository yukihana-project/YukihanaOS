// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig;

internal static class FeatureDependencies
{
    /// <summary>
    /// Feature and everything it requires, transitively. Requirements come before the feature.
    /// </summary>
    public static List<string> WithRequirements(ManifestConfig manifest, string id)
    {
        List<string> result = [];
        Collect(manifest, id, result, []);
        return result;
    }

    /// <summary>
    /// Features that list <paramref name="id"/> directly in their depends.
    /// </summary>
    public static List<string> RequiredBy(ManifestConfig manifest, string id)
    {
        return [.. manifest.Feature.Where(f => f.Depends.Contains(id)).Select(f => f.Id)];
    }

    /// <summary>
    /// Enabled features that list <paramref name="id"/> directly in their depends.
    /// </summary>
    public static List<string> EnabledDependants(ManifestConfig manifest, string id, IEnumerable<string> enabled)
    {
        HashSet<string> on = [.. enabled];
        return [.. RequiredBy(manifest, id).Where(on.Contains)];
    }

    private static void Collect(ManifestConfig manifest, string id, List<string> result, HashSet<string> seen)
    {
        // Cycles are rejected by validation; the seen set only keeps this walk from looping
        if (!seen.Add(id))
        {
            return;
        }

        ManifestConfig.FeatureConfig feature = manifest.Feature.First(f => f.Id == id);

        foreach (string dependency in feature.Depends)
        {
            Collect(manifest, dependency, result, seen);
        }

        result.Add(id);
    }
}
