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

        // Sources are relative to the kernel project, which is where the tool lives
        foreach (ManifestConfig.FeatureConfig feature in ConfigManager.ManifestConfig.Feature)
        {
            if (feature.Exclude is { Count: > 0 })
            {
                Log.Error("Feature '{Feature}' uses 'exclude', which was replaced by 'sources'. List the files the feature owns instead", feature.Id);
                return 1;
            }

            foreach (string source in feature.Sources)
            {
                if (!File.Exists(Path.Combine(Configuration.BaseDirectory, source)))
                {
                    Log.Error("Feature '{Feature}' lists source '{Source}', which does not exist", feature.Id, source);
                    return 1;
                }
            }
        }

        string? sharedSource = ConfigManager.ManifestConfig.Feature.SelectMany(f => f.Sources).GroupBy(s => s).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (sharedSource is not null)
        {
            Log.Error("Source '{Source}' belongs to more than one feature", sharedSource);
            return 1;
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

        string[] allIds = [.. manifestFeatureIds, .. ConfigManager.ManifestConfig.Option.Select(o => o.Id)];
        string? duplicate = allIds.GroupBy(i => i).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicate is not null)
        {
            Log.Error("Id '{Id}' is used by more than one feature or option", duplicate);
            return 1;
        }

        foreach (ManifestConfig.OptionConfig option in ConfigManager.ManifestConfig.Option)
        {
            if (!ConfigManager.ManifestConfig.Group.Any(g => g.Id == option.Group))
            {
                Log.Error("Option '{Option}' references unknown group '{Group}'", option.Id, option.Group);
                return 1;
            }

            if (option.Type is not ("string" or "int"))
            {
                Log.Error("Option '{Option}' has unknown type '{Type}'", option.Id, option.Type);
                return 1;
            }

            if (option.Default is not null && FeatureOptions.CheckValue(option, option.Default) is { } problem)
            {
                Log.Error("Option '{Option}' default: {Problem}", option.Id, problem);
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
