// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Tomlyn;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class FeatureCommandHandler
{
    public static int HandleEnable(ParseResult result)
    {
        string? feature = result.GetValue(Globals.Args.FeatureArgument);

        if (feature is null)
        {
            Log.Fatal("Please, specify the feature id");
            return 1;
        }

        ConfigManager.LoadConfigs();

        if (ConfigManager.CurrentConfig!.Enabled is null)
        {
            ConfigManager.CurrentConfig!.Enabled = [];
        }

        bool isValidFeature = ConfigManager.ManifestConfig!.Feature.Any(f => f.Id == feature);

        if (!isValidFeature)
        {
            Log.Fatal("This feature is not declared in Manifest.toml");
            return 1;
        }

        ConfigManager.CurrentConfig!.Enabled.Add(feature);

        using FileStream fs = File.Open(Globals.GetManifestClosePath("Current.toml"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        TomlSerializer.Serialize(fs, ConfigManager.CurrentConfig, CurrentConfigContext.Default);

        return 0;
    }
    public static int HandleDisable(ParseResult result)
    {
        string? feature = result.GetValue(Globals.Args.FeatureArgument);

        if (feature is null)
        {
            Log.Fatal("Please, specify the feature id");
            return 1;
        }

        ConfigManager.LoadConfigs();

        if (ConfigManager.CurrentConfig!.Enabled is null)
        {
            return 0;
        }

        bool isValidFeature = ConfigManager.ManifestConfig!.Feature.Any(f => f.Id == feature);

        if (!isValidFeature)
        {
            Log.Fatal("This feature is not declared in Manifest.toml");
            return 1;
        }

        if (!ConfigManager.CurrentConfig.Enabled.Contains(feature))
        {
            return 0;
        }

        ConfigManager.CurrentConfig!.Enabled.Remove(feature);

        using FileStream fs = File.Open(Globals.GetManifestClosePath("Current.toml"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        TomlSerializer.Serialize(fs, ConfigManager.CurrentConfig, CurrentConfigContext.Default);

        return 0;
    }
}
