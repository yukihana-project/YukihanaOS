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
        ConfigManager.LoadConfigs();

        string[] manifestFeatureIds = [.. ConfigManager.ManifestConfig!.Feature.Select(f => f.Id)];
        foreach((string preset, PresetConfig cfg) in ConfigManager.PresetConfigs)
        {
            Log.Verbose("Valdating '{PresetName}'.", preset);

            string[] unknown = [.. cfg.Enabled.Where(e => manifestFeatureIds.Contains(e))];

            if (unknown.Length > 0)
            {
                Log.Fatal("Unknown features id found while validating '{PresetName}': ", preset);
                foreach(string feat in unknown)
                {
                    Log.Fatal("  {FeatureId}", feat);
                }

                Environment.Exit(1);
            }
        }

        Log.Information("Manifest and presets were validated.");

        return 0;
    }
}