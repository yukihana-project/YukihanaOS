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

        List<string> toEnable = [.. FeatureDependencies.WithSelected(ConfigManager.ManifestConfig!, feature)
            .Where(id => !ConfigManager.CurrentConfig!.Enabled.Contains(id))];

        // Enabling a choice member switches off its sibling. Selected members never switch, so a clash is refused
        ManifestConfig.ChoiceConfig? choice = FeatureDependencies.ChoiceOf(ConfigManager.ManifestConfig!, feature);
        List<string> switchedOff = [.. ConfigManager.CurrentConfig!.Enabled.Where(id => choice is not null && choice.Members.Contains(id) && id != feature)];

        List<string> after = [.. ConfigManager.CurrentConfig!.Enabled.Where(id => !switchedOff.Contains(id)), .. toEnable];

        if (FeatureDependencies.FindProblem(ConfigManager.ManifestConfig!, after) is { } problem)
        {
            Log.Fatal("Cannot enable '{Feature}': {Problem}", feature, problem);
            return 1;
        }

        ConfigManager.CurrentConfig!.Enabled.RemoveAll(switchedOff.Contains);
        ConfigManager.CurrentConfig!.Enabled.AddRange(toEnable);

        if (switchedOff.Count > 0)
        {
            Log.Information("Switched off {Features} (same choice)", string.Join(", ", switchedOff));
        }

        if (toEnable.Count > 1)
        {
            Log.Information("Also enabled selected features: {Features}", string.Join(", ", toEnable.Where(id => id != feature)));
        }

        SaveAndRegenerate();

        return 0;
    }
    private static void SaveAndRegenerate()
    {
        // Feature list no longer matches the preset, so the preset name is dropped
        ConfigManager.CurrentConfig!.Config = null;

        using (FileStream fs = File.Open(Configuration.CurrentTomlPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            TomlSerializer.Serialize(fs, ConfigManager.CurrentConfig, CurrentConfigContext.Default);
        }

        SourceGenerator.GenerateFromCurrent();
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

        List<string> dependants = FeatureDependencies.EnabledDependants(ConfigManager.ManifestConfig!, feature, ConfigManager.CurrentConfig.Enabled!);

        if (dependants.Count > 0)
        {
            Log.Fatal("Feature '{Feature}' is required by enabled feature(s): {Dependants}", feature, string.Join(", ", dependants));
            return 1;
        }

        List<string> remaining = [.. ConfigManager.CurrentConfig.Enabled.Where(id => id != feature)];

        if (FeatureDependencies.FindProblem(ConfigManager.ManifestConfig!, remaining) is { } problem)
        {
            Log.Fatal("Cannot disable '{Feature}': {Problem}", feature, problem);
            return 1;
        }

        ConfigManager.CurrentConfig!.Enabled.Remove(feature);

        SaveAndRegenerate();

        return 0;
    }
}
