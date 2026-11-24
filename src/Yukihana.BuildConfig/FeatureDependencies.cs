// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig;

internal static class FeatureDependencies
{
    /// <summary>
    /// Feature and everything it selects, transitively. Selected features come before the feature.
    /// </summary>
    public static List<string> WithSelected(ManifestConfig manifest, string id)
    {
        List<string> result = [];
        Collect(manifest, id, result, []);
        return result;
    }

    /// <summary>
    /// Features that depend on or select <paramref name="id"/> directly.
    /// </summary>
    public static List<string> RequiredBy(ManifestConfig manifest, string id)
    {
        return [.. manifest.Feature.Where(f => f.Depends.Contains(id) || f.Select.Contains(id)).Select(f => f.Id)];
    }

    /// <summary>
    /// Enabled features that depend on or select <paramref name="id"/> directly.
    /// </summary>
    public static List<string> EnabledDependants(ManifestConfig manifest, string id, IEnumerable<string> enabled)
    {
        HashSet<string> on = [.. enabled];
        return [.. RequiredBy(manifest, id).Where(on.Contains)];
    }

    /// <summary>
    /// First problem in an enabled set: an unmet depends or select, or a choice without exactly one member on.
    /// Null when the set is valid. Unknown ids are skipped.
    /// </summary>
    public static string? FindProblem(ManifestConfig manifest, IEnumerable<string> enabled)
    {
        HashSet<string> on = [.. enabled];

        foreach (string id in on)
        {
            ManifestConfig.FeatureConfig? feature = manifest.Feature.FirstOrDefault(f => f.Id == id);
            string? missing = feature?.Depends.Concat(feature.Select).FirstOrDefault(d => !on.Contains(d));

            if (missing is not null)
            {
                return $"'{id}' requires '{missing}', which is not enabled";
            }
        }

        foreach (ManifestConfig.ChoiceConfig choice in manifest.Choice)
        {
            List<string> chosen = [.. choice.Members.Where(on.Contains)];

            if (chosen.Count > 1)
            {
                return $"choice '{choice.Id}' allows one feature, but {string.Join(", ", chosen)} are enabled";
            }

            if (chosen.Count == 0 && !choice.Optional)
            {
                return $"choice '{choice.Id}' needs one of: {string.Join(", ", choice.Members)}";
            }
        }

        return null;
    }

    /// <summary>
    /// Starting selection: features enabled by default and each choice's default, with what they select.
    /// </summary>
    public static List<string> Defaults(ManifestConfig manifest)
    {
        List<string> roots = [.. manifest.Feature.Where(f => f.EnabledByDefault == true).Select(f => f.Id),
            .. manifest.Choice.Where(c => c.Default is not null).Select(c => c.Default!)];

        return [.. roots.SelectMany(id => WithSelected(manifest, id)).Distinct()];
    }

    /// <summary>
    /// The choice a feature belongs to, or null.
    /// </summary>
    public static ManifestConfig.ChoiceConfig? ChoiceOf(ManifestConfig manifest, string id)
    {
        return manifest.Choice.FirstOrDefault(c => c.Members.Contains(id));
    }

    private static void Collect(ManifestConfig manifest, string id, List<string> result, HashSet<string> seen)
    {
        // Cycles are rejected by validation; the seen set only keeps this walk from looping
        if (!seen.Add(id))
        {
            return;
        }

        ManifestConfig.FeatureConfig feature = manifest.Feature.First(f => f.Id == id);

        foreach (string selected in feature.Select)
        {
            Collect(manifest, selected, result, seen);
        }

        result.Add(id);
    }
}
