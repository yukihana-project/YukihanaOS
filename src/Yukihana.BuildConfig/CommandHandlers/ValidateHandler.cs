// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class ValidateHandler
{
    public static int Handle(ParseResult result)
    {
        return Validate();
    }

    public static int Validate()
    {
        ConfigManager.LoadConfigs(false);

        string[] manifestFeatureIds = [.. ConfigManager.ManifestConfig!.Feature.Select(f => f.Id)];

        foreach (ManifestConfig.FeatureConfig feature in ConfigManager.ManifestConfig.Feature)
        {
            string? unknownDep = feature.Depends.FirstOrDefault(d => !manifestFeatureIds.Contains(d));
            if (unknownDep is not null)
            {
                Log.Error("Feature '{Feature}' depends on unknown feature '{Dependency}'", feature.Id, unknownDep);
                return 1;
            }
        }

        try
        {
            new DependencyGraph().BuildDependencyGraph([.. ConfigManager.ManifestConfig.Feature]);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error("{Message}", ex.Message);
            return 1;
        }
        foreach ((string preset, PresetConfig cfg) in ConfigManager.PresetConfigs)
        {
            Log.Verbose("Valdating {PresetName}.", preset);

            string[] unknown = [.. cfg.Enabled.Where(e => !manifestFeatureIds.Contains(e))];

            if (unknown.Length > 0)
            {
                Log.Error("Unknown features id found while validating {PresetName}: ", preset);
                foreach (string feat in unknown)
                {
                    Log.Error("  {FeatureId}", feat);
                }

                Log.Error("Available features:");
                foreach (string feat in manifestFeatureIds)
                {
                    Log.Error("  {ManifestFeatureId}", feat);
                }

                return 1;
            }
        }

        foreach ((string preset, PresetConfig cfg) in ConfigManager.PresetConfigs)
        {
            foreach (string id in cfg.Enabled)
            {
                ManifestConfig.FeatureConfig feature = ConfigManager.ManifestConfig!.Feature.First(f => f.Id == id);
                string? missing = feature.Depends.FirstOrDefault(d => !cfg.Enabled.Contains(d));

                if (missing is not null)
                {
                    Log.Error("Preset '{PresetName}': feature '{Feature}' requires '{Dependency}', which is not enabled", preset, id, missing);
                    return 1;
                }
            }
        }

        Log.Information("Manifest and presets were validated.");
        return 0;
    }
}
