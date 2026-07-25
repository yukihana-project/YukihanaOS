// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class ListCommandHandler
{
    // "features", "groups", "presets", "enabled", "disabled"
    public static int Handle(ParseResult result)
    {
        string? arg = result.GetValue(Globals.Args.FeatureArgument);

        if (arg is null)
        {
            Log.Fatal("No argument specified.");
            return 1;
        }

        switch (arg)
        {
            case "features":
                ConfigManager.LoadConfigs(false);
                string[] features = [.. ConfigManager.ManifestConfig!.Feature.Select(s => $"id: {s.Id}; name: {s.Name}")];

                if (features.Length == 0)
                {
                    Log.Information("(No features are defined)");
                    break;
                }

                foreach (string feature in features)
                {
                    Log.Information(feature);
                }
                break;

            case "groups":
                ConfigManager.LoadConfigs(false);
                string[] groups = [.. ConfigManager.ManifestConfig!.Group.Select(s => $"id: {s.Id}; name: {s.Name}")];

                if (groups.Length == 0)
                {
                    Log.Information("(No groups are defined)");
                    break;
                }

                foreach (string group in groups)
                {
                    Log.Information(group);
                }
                break;

            case "presets":
                ConfigManager.LoadConfigs(false);
                string[] presets = [.. Directory.EnumerateFiles(Globals.ConfigsDirectoryPath).Select(s => Path.GetFileNameWithoutExtension(s))];

                if (presets.Length == 0)
                {
                    Log.Information("(No presets are present)");
                    break;
                }

                foreach (string preset in presets)
                {
                    Log.Information(preset);
                }
                break;

            case "enabled":
                ConfigManager.LoadConfigs(true);
                string[] enabledFeatures = [.. ConfigManager.CurrentConfig!.Enabled ?? []];

                if (enabledFeatures.Length == 0)
                {
                    Log.Information("(No features are enabled)");
                    break;
                }

                foreach (string enabled in enabledFeatures)
                {
                    Log.Information(enabled);
                }
                break;

            case "disabled":
                ConfigManager.LoadConfigs(true);

                List<ManifestConfig.FeatureConfig> featureCollection = ConfigManager.ManifestConfig!.Feature ?? [];

                string[] disabledFeatures = [.. featureCollection.Select(f => f.Id).Where(f => ConfigManager.CurrentConfig!.Enabled!.Contains(f))];

                if (disabledFeatures.Length == 0)
                {
                    Log.Information("(No features are disabled)");
                    break;
                }

                foreach (string disabled in disabledFeatures)
                {
                    Log.Information(disabled);
                }
                break;
        }

        return 0;
    }
}
