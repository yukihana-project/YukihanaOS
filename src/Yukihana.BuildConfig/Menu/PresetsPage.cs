// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Spectre.Console;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.Menu;

internal static class PresetsPage
{
    public static Dictionary<string, bool>? Show()
    {
        ConfigManager.LoadConfigs(false);

        return Process();
    }

    private static Dictionary<string, bool>? Process()
    {
        AnsiConsole.Clear();

        Dictionary<string, PresetConfig> presets = ConfigManager.PresetConfigs;

        const string Back = "[gray]<Back>[/]";
        const string Exit = "[gray]<Exit>[/]";

        DrawHeader();

        string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("YKConfig selection")
            .EnableSearch()
            .PageSize(15)
            .MoreChoicesText("[gray](More items available)[/]")
            .AddChoiceGroup("Presets", presets.Keys)
            .AddChoiceGroup("Commands", [Back, Exit]));

        switch (selected)
        {
            case Back:
                return null;

            case Exit:
                AnsiConsole.Clear();
                Environment.Exit(0);
                break;
        }

        PresetConfig selectedPreset = presets[selected];

        Dictionary<string, bool> features = ConfigManager.ManifestConfig!.Feature.ToDictionary(f => f.Id, f => selectedPreset.Enabled.Contains(f.Id));

        return features;
    }

    private static void DrawHeader()
    {
        AnsiConsole.MarkupLine("[aqua]Yukihana YKConfig[/]\n\n\n");
    }
}
