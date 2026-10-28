// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class InfoCommandHandler
{
    public static int Handle(ParseResult result)
    {
        string? feature = result.GetValue(Globals.Args.FeatureArgument);

        if (feature is null)
        {
            Log.Fatal("Please, specify feature");
            return 1;
        }

        ConfigManager.LoadConfigs(false);

        ManifestConfig.FeatureConfig? cfg = ConfigManager.ManifestConfig!.Feature?.FirstOrDefault(f => f.Id == feature);

        if (cfg is null)
        {
            Log.Fatal("Specified feature is not defined in Manifest.toml");
            return 1;
        }

        Log.Information("Feature information:");
        Log.Information("Id: {FeatureId}", cfg.Id);
        Log.Information("Display name: {FeatureName}", cfg.Name);
        Log.Information("Description: {FeatureDesc}", cfg.Description);
        Log.Information("Parent group: {FeatureGroup}", cfg.Group);
        Log.Information("C# define: {FeatureDefine}", cfg.Define);
        Log.Information("Choice: {FeatureChoice}", FeatureDependencies.ChoiceOf(ConfigManager.ManifestConfig!, cfg.Id)?.Id ?? "none");
        Log.Information("Enabled by default: {FeatureDefault}", FeatureDependencies.Defaults(ConfigManager.ManifestConfig!).Contains(cfg.Id) ? "yes" : "no");

        Log.Information("Depends:");

        foreach (string depends in cfg.Depends)
        {
            Log.Information("  {FeatureDepends}", depends);
        }

        Log.Information("Selects:");

        foreach (string select in cfg.Select)
        {
            Log.Information("  {FeatureSelect}", select);
        }

        Log.Information("Required by:");

        foreach (string dependant in FeatureDependencies.RequiredBy(ConfigManager.ManifestConfig!, cfg.Id))
        {
            Log.Information("  {FeatureDependant}", dependant);
        }

        Log.Information("Sources (compiled only when enabled):");

        foreach (string source in cfg.Sources)
        {
            Log.Information("  {FeatureSource}", source);
        }

        return 0;
    }
}
