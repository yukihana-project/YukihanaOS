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

        ManifestConfig.FeatureConfig? cfg = ConfigManager.ManifestConfig!.Feature?.First(f => f.Id == feature);

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

        Log.Information("Depends:");

        foreach (string depends in cfg.Depends)
        {
            Log.Information("  {FeatureDepends}", depends);
        }

        Log.Information("Excluded sources:");

        foreach (string exclude in cfg.Exclude)
        {
            Log.Information("  {FeatureExclude}", exclude);
        }

        return 0;
    }
}
