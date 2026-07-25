// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using System.Security.Cryptography;
using Serilog;
using Tomlyn;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class CheckCommandHandler
{
    internal static int Handle(ParseResult result)
    {
        bool doFix = result.GetValue(Globals.Args.FixOption);

        ConfigManager.LoadConfigs(true);

        if (!File.Exists(Globals.GetManifestClosePath("State.toml")) || ConfigManager.StateConfig is null)
        {
            Log.Fatal("No configuration was generated. Please, generate configuration before checking");
            return 1;
        }

        StateConfig state = ConfigManager.StateConfig;

        if (state.GeneratorVersion < typeof(Program).Assembly.GetName().Version)
        {
            Log.Warning("The configuration was made by older version of ykconfig ({OldVersion} < {CurrentVersion})",
                state.GeneratorVersion, typeof(Program).Assembly.GetName().Version);

            if (!doFix)
            {
                return 1;
            }

            Log.Information("Updating current configuration");

            SourceGenerator.GenerateFromCurrent();
            return 0;
        }

        SHA256 sha256 = SHA256.Create();

        byte[] manifestHash;
        byte[] currentHash;

        using (FileStream manifestStream = File.OpenRead(Globals.ManifestTomlPath))
        {
            manifestHash = sha256.ComputeHash(manifestStream);
        }
        using (FileStream currentStream = File.OpenRead(Globals.GetManifestClosePath("Current.toml")))
        {
            currentHash = sha256.ComputeHash(currentStream);
        }

        byte[] manifestChecksum = Convert.FromHexString(state.ManifestHash);
        byte[] currentChecksum = Convert.FromHexString(state.ConfigurationHash);

        if (!manifestChecksum.AsSpan().SequenceEqual(manifestHash))
        {
            Log.Warning("The Manifest.toml checksum missmatched");

            if (!doFix)
            {
                return 1;
            }

            return Regenerate();
        }

        if (!currentChecksum.AsSpan().SequenceEqual(currentHash))
        {
            Log.Warning("The Current.toml checksum missmatched");

            if (!doFix)
            {
                return 1;
            }

            return Regenerate();
        }

        return 0;
    }

    private static int Regenerate()
    {
        if (ValidateHandler.Validate() != 0)
        {
            Log.Fatal("Cannot generate sources with unknown features");
            return 1;
        }

        string? presetName = ConfigManager.CurrentConfig!.Config;

        if (presetName is not null)
        {
            Log.Information("Has perset defined, fetching features for current preset");

            if (!ConfigManager.PresetConfigs.TryGetValue(presetName!, out PresetConfig? preset))
            {
                Log.Fatal("Unable to fetech preset '{PresetName}'", presetName);
                return 1;
            }

            CurrentConfig newCurrent = new()
            {
                Enabled = [.. ConfigManager.ManifestConfig!.Feature.Where(f => preset.Enabled.Contains(f.Id)).Select(f => f.Id)],
                Config = presetName
            };

            using (FileStream fs = File.Open(Globals.GetManifestClosePath("Current.toml"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                TomlSerializer.Serialize(fs, newCurrent, CurrentConfigContext.Default);
            }
        }
        else
        {
            Log.Warning("No preset defined. Proceeding with features as is");
        }

        SourceGenerator.GenerateFromCurrent();
        return 0;
    }
}
