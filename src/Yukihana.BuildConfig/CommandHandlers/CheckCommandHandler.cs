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
        bool regenState = result.GetValue(Globals.Args.RegenerateStateOption);

        ConfigManager.LoadConfigs(true);

        if (FeatureDependencies.FindProblem(ConfigManager.ManifestConfig!, ConfigManager.CurrentConfig!.Enabled ?? []) is { } problem)
        {
            Log.Fatal("Current.toml is invalid: {Problem}", problem);
            return 1;
        }

        if (regenState)
        {
            return RegenerateState();
        }

        if (!File.Exists(Configuration.StateTomlPath))
        {
            Log.Fatal("No configuration was generated. Please, generate configuration before checking");
            return 1;
        }

        if (ConfigManager.StateConfig is null)
        {
            Log.Warning("State.toml could not be read");

            if (!doFix)
            {
                Log.Information("Run 'check --fix' or 'check --regenerate-state' to rebuild it");
                return 1;
            }

            return RegenerateFromCurrent();
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

            if (ValidateHandler.Validate() != 0)
            {
                return 1;
            }

            SourceGenerator.GenerateFromCurrent();
            return 0;
        }

        SHA256 sha256 = SHA256.Create();

        byte[] manifestHash;
        byte[] currentHash;

        using (FileStream manifestStream = File.OpenRead(Configuration.ManifestTomlPath))
        {
            manifestHash = sha256.ComputeHash(manifestStream);
        }
        using (FileStream currentStream = File.OpenRead(Configuration.CurrentTomlPath))
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

        if (!SourceGenerator.GeneratedFilesMatchCurrent())
        {
            Log.Warning("The generated files do not match Current.toml");

            if (!doFix)
            {
                return 1;
            }

            // Current.toml is the source of truth here, so regenerate without re-applying the preset
            return RegenerateFromCurrent();
        }

        return 0;
    }

    private static int RegenerateFromCurrent()
    {
        if (ValidateHandler.Validate() != 0)
        {
            return 1;
        }

        SourceGenerator.GenerateFromCurrent();
        return 0;
    }

    private static int RegenerateState()
    {
        if (!SourceGenerator.GeneratedFilesMatchCurrent())
        {
            Log.Fatal("The generated files do not match Current.toml. Run 'check --fix' first");
            return 1;
        }

        SourceGenerator.WriteState();
        Log.Information("Regenerated State.toml");
        return 0;
    }

    private static int Regenerate()
    {
        if (ValidateHandler.Validate() != 0)
        {
            Log.Fatal("Cannot generate sources: Manifest.toml or a preset is invalid (see the errors above)");
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
                Config = presetName,
                Options = ConfigManager.CurrentConfig!.Options
            };

            using (FileStream fs = File.Open(Configuration.CurrentTomlPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                TomlSerializer.Serialize(fs, newCurrent, CurrentConfigContext.Default);
            }

            // Generator reads the in-memory config, so it has to match what was just written
            ConfigManager.CurrentConfig = newCurrent;
        }
        else
        {
            Log.Warning("No preset defined. Proceeding with features as is");
        }

        SourceGenerator.GenerateFromCurrent();
        return 0;
    }
}
