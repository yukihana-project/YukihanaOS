// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Microsoft.Win32.SafeHandles;
using Serilog;
using Tomlyn;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig;

internal static class ConfigManager
{
    public static CurrentConfig? CurrentConfig { get; set; } = new();
    public static ManifestConfig? ManifestConfig { get; set; } = new();
    public static Dictionary<string, PresetConfig> PresetConfigs { get; set; } = [];
    public static StateConfig? StateConfig { get; set; } = new();
    private static bool s_hasLoadedConfigs = false;

    public static void LoadConfigs(bool ensureHasState = true)
    {
        if (s_hasLoadedConfigs)
        {
            return;
        }

        s_hasLoadedConfigs = true;

        try
        {
            using (FileStream fs = File.Open(Globals.ManifestTomlPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                ManifestConfig = TomlSerializer.Deserialize<ManifestConfig>(fs, ManifestConfigContext.Default);
            }

            foreach(string file in Directory.GetFiles(Globals.ConfigsDirectoryPath))
            {
                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    PresetConfig? preset = TomlSerializer.Deserialize<PresetConfig>(fs, PresetConfigContext.Default);

                    if (preset is null)
                    {
                        Log.Error("Unable to load preset {PresetPath}. Is it valid toml?", file);
                        continue;
                    }

                    PresetConfigs.Add(Path.GetFileNameWithoutExtension(file), preset);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unable to load configs.");
            Environment.Exit(1);
        }

        if (!ensureHasState)
        {
            return;
        }

        try
        {
            using (FileStream fs = File.Open(Globals.GetManifestClosePath("Current.toml"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                CurrentConfig = TomlSerializer.Deserialize<CurrentConfig>(fs, CurrentConfigContext.Default);
            }
            using (FileStream fs = File.Open(Globals.GetManifestClosePath("State.toml"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                StateConfig = TomlSerializer.Deserialize<StateConfig>(fs, StateConfigContext.Default);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unable to load current state.");
            Environment.Exit(1);
        }

        if (CurrentConfig is null)
        {
            Log.Fatal("Unable to parse Current.toml. Please, configure the build first");
            Environment.Exit(1);
        }

        if (StateConfig is null)
        {
            Log.Fatal("Unable to parse State.toml. Please, configure the build first");
            Environment.Exit(1);
        }
    }

    public static void UpdateCurrentConfig(Dictionary<string, bool> features)
    {
        CurrentConfig = new()
        {
            Enabled = [.. features.Where(kv => kv.Value).Select(kv => kv.Key)],
        };

        try
        {
            using FileStream fs = File.Open(
                Globals.GetManifestClosePath("Current.toml"),
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            TomlSerializer.Serialize(fs, CurrentConfig, CurrentConfigContext.Default);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unable to save CurrentConfig.");
            Environment.Exit(1);
        }
    }
}