// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Serilog;
using Tomlyn;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class PresetCommandHandler
{
    public static int HandleList(ParseResult result)
    {
        string[] files = [.. Directory.EnumerateFiles(Globals.ConfigsDirectoryPath).Select(f => Path.GetFileNameWithoutExtension(f))];

        foreach (string file in files)
        {
            Log.Information(file);
        }
        return 0;
    }
    public static int HandleShow(ParseResult result)
    {
        string? name = result.GetValue(Globals.Args.PresetInteractionArgument);

        if (name is null)
        {
            Log.Fatal("Please, specify preset name");
            return 1;
        }

        string path = Path.Combine(Globals.ConfigsDirectoryPath, $"{name}.toml");

        if (!File.Exists(path))
        {
            Log.Fatal("Unable to locate preset at {PresetPath}", path);
            return 1;
        }

        using FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
        PresetConfig? cfg = TomlSerializer.Deserialize<PresetConfig>(fs, PresetConfigContext.Default);

        if (cfg is null)
        {
            Log.Fatal("Failed to read preset. Check if it's valid TOML");
            return 1;
        }

        Log.Information("Preset information: ");
        Log.Information("Name: {PresetName}", name);
        Log.Information("Description: {PresetDescription}", cfg.Description);
        Log.Information("Enabled:");

        foreach (string enabled in cfg.Enabled)
        {
            Log.Information("  {EnabledFeature}", enabled);
        }

        return 0;
    }
}
