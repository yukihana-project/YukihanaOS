// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Tomlyn;
using Yukihana.BuildConfig.Menu;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class ConfigureManager
{
    public static int Handle(ParseResult result)
    {
        string? presetName = result.GetValue(Globals.Args.PresetArgument);
        bool isInteractive = result.GetValue(Globals.Args.InteractiveOption);
        bool doClean = result.GetValue(Globals.Args.CleanOption);
        //bool doNotSave = result.GetValue(Globals.Args.NoSaveOption);

        ConfigManager.LoadConfigs(false);

        if (string.IsNullOrWhiteSpace(presetName) && !isInteractive)
        {
            Log.Fatal("Cannot configure emtpy preset. Please, specify preset, or use '-i' flag");
            return 1;
        }

        if (isInteractive)
        {
            ConfigPage.Show();
            return 0;
        }

        if (!ConfigManager.PresetConfigs.TryGetValue(presetName!, out PresetConfig? preset))
        {
            Log.Fatal("Unable to fetech preset '{PresetName}'", presetName);
            return 1;
        }

        if (doClean)
        {
            foreach (string file in Directory.EnumerateFiles(Globals.OutputDirectoryPath))
            {
                File.Delete(file);
            }
        }

        CurrentConfig newCurrent = new()
        {
            Enabled = [.. ConfigManager.ManifestConfig!.Feature.Where(f => preset.Enabled.Contains(f.Id)).Select(f => f.Id)]
        };

        using FileStream fs = File.Open(Globals.GetManifestClosePath("Current.toml"), FileMode.Create, FileAccess.Write, FileShare.None);

        TomlSerializer.Serialize(fs, newCurrent, CurrentConfigContext.Default);

        // TODO: Genertate configs

        return 0;
    }
}