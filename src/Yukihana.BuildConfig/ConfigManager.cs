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
    private static bool s_hasLoadedConfigState = false;

    public static void LoadConfigs(bool ensureHasState = true)
    {
        if (s_hasLoadedConfigs)
        {
            if (!s_hasLoadedConfigState && ensureHasState)
            {
                goto __load_state;
            }
            return;
        }

        s_hasLoadedConfigs = true;

        try
        {
            using (FileStream fs = File.Open(Configuration.ManifestTomlPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                ManifestConfig = TomlSerializer.Deserialize<ManifestConfig>(fs, ManifestConfigContext.Default);
            }

            foreach (string file in Directory.GetFiles(Configuration.ConfigsDirectoryPath))
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

    __load_state:

        s_hasLoadedConfigState = true;

        try
        {
            using (FileStream fs = File.Open(Configuration.CurrentTomlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                CurrentConfig = TomlSerializer.Deserialize<CurrentConfig>(fs, CurrentConfigContext.Default);
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

        try
        {
            using (FileStream fs = File.Open(Configuration.StateTomlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                StateConfig = TomlSerializer.Deserialize<StateConfig>(fs, StateConfigContext.Default);
            }
        }
        catch
        {

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
                Configuration.CurrentTomlPath,
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
