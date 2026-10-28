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
            string? unknownDep = feature.Depends.Concat(feature.Select).FirstOrDefault(d => !manifestFeatureIds.Contains(d));
            if (unknownDep is not null)
            {
                Log.Error("Feature '{Feature}' refers to unknown feature '{Dependency}'", feature.Id, unknownDep);
                return 1;
            }
        }

        foreach (ManifestConfig.ChoiceConfig choice in ConfigManager.ManifestConfig.Choice)
        {
            if (choice.Members.Count == 0)
            {
                Log.Error("Choice '{Choice}' has no members", choice.Id);
                return 1;
            }

            string? unknownMember = choice.Members.FirstOrDefault(m => !manifestFeatureIds.Contains(m));
            if (unknownMember is not null)
            {
                Log.Error("Choice '{Choice}' refers to unknown feature '{Member}'", choice.Id, unknownMember);
                return 1;
            }

            string? shared = choice.Members.FirstOrDefault(m => ConfigManager.ManifestConfig.Choice.Count(c => c.Members.Contains(m)) > 1);
            if (shared is not null)
            {
                Log.Error("Feature '{Feature}' belongs to more than one choice", shared);
                return 1;
            }

            if (choice.Default is not null && !choice.Members.Contains(choice.Default))
            {
                Log.Error("Choice '{Choice}' default '{Default}' is not one of its members", choice.Id, choice.Default);
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
            if (FeatureDependencies.FindProblem(ConfigManager.ManifestConfig!, cfg.Enabled) is { } problem)
            {
                Log.Error("Preset '{PresetName}': {Problem}", preset, problem);
                return 1;
            }
        }

        if (FeatureDependencies.FindProblem(ConfigManager.ManifestConfig!, FeatureDependencies.Defaults(ConfigManager.ManifestConfig!)) is { } defaultsProblem)
        {
            Log.Error("Defaults are invalid: {Problem}", defaultsProblem);
            return 1;
        }

        Log.Information("Manifest and presets were validated.");
        return 0;
    }
}
