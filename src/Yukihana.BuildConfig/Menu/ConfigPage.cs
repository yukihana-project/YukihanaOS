// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Serilog;
using Spectre.Console;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.Menu;

// imma cry T_T
internal static class ConfigPage
{
    // Shown on the next redraw, since the prompt clears the screen each time
    private static string? s_notice;

    public static void Show()
    {
        ConfigManager.LoadConfigs(false);

        // Start from the saved selection when there is one, otherwise from the defaults
        ManifestConfig manifest = ConfigManager.ManifestConfig!;
        List<string> start = FeatureDependencies.Defaults(manifest);

        if (File.Exists(Configuration.CurrentTomlPath))
        {
            ConfigManager.LoadConfigs(true);
            start = [.. ConfigManager.CurrentConfig!.Enabled ?? []];
        }

        ManifestTree tree = ManifestTreeBuilder.Build(manifest);
        Dictionary<string, bool> featureStates = tree.GetFeatureStates(start);
        Dictionary<string, string?> optionValues = manifest.Option.ToDictionary(o => o.Id, o => FeatureOptions.Effective(o, ConfigManager.CurrentConfig?.Options));

        GroupNode? node = null;

        while (true)
        {
            node = Process(tree, node, featureStates, optionValues); ;
        }
    }

    private static GroupNode? Process(ManifestTree tree, GroupNode? current, Dictionary<string, bool> featureStates, Dictionary<string, string?> optionValues)
    {
        AnsiConsole.Clear();

        string pathNames = "Root";
        string path = string.Empty;
        GroupNode? group = null;

        List<GroupNode> groups = tree.RootGroups;
        List<ManifestConfig.FeatureConfig> features = [];
        List<ManifestConfig.OptionConfig> options = [];

        if (current is not null)
        {
            pathNames = "Root -> " + tree.GetNamePath(current);
            group = tree.GetNodeByIdPath(tree.GetIdPath(current));

            groups = group!.Children;
            features = group.Features;
            options = group.Options;
        }

        DrawHeader(pathNames);

        string[] groupNames = [.. groups.Select(g => "    " + g.Group.Name)];
        Dictionary<string, ManifestConfig.FeatureConfig> featureLabels = [];
        foreach (ManifestConfig.FeatureConfig feat in features.Where(f => featureStates.ContainsKey(f.Id)))
        {
            featureLabels[FeatureLabel(ConfigManager.ManifestConfig!, feat, featureStates)] = feat;
        }

        string[] featureNames = [.. featureLabels.Keys];

        Dictionary<string, ManifestConfig.OptionConfig> optionLabels = [];
        foreach (ManifestConfig.OptionConfig opt in options)
        {
            optionLabels[OptionLabel(opt, optionValues[opt.Id])] = opt;
        }

        string[] optionNames = [.. optionLabels.Keys];

        const string Back = "[gray]<Back>[/]";
        const string Save = "[gray]<Save>[/]";
        const string Presets = "[gray]<Presets>[/]";
        const string Exit = "[gray]<Exit>[/]";

        string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("YKConfig selection")
            .EnableSearch()
            .PageSize(15)
            .MoreChoicesText("[gray](More items available)[/]")
            .AddChoiceGroup("Config", [.. groupNames, .. featureNames, .. optionNames])
            .AddChoiceGroup("Commands", [Back, Save, Presets, Exit]));

        switch (selected)
        {
            case Back:
                if (current is null)
                {
                    return null;
                }
                else
                {
                    return tree.GetParent(current);
                }
            case Save:
                if (FeatureDependencies.FindProblem(ConfigManager.ManifestConfig!, featureStates.Where(kv => kv.Value).Select(kv => kv.Key)) is { } problem)
                {
                    s_notice = $"[yellow]Not saved: {problem}[/]";
                    break;
                }

                // Same rule as 'feature set': a value equal to the default is stored as absence
                Dictionary<string, string> stored = [];
                foreach (ManifestConfig.OptionConfig opt in ConfigManager.ManifestConfig!.Option)
                {
                    string? value = optionValues[opt.Id];

                    if (value != opt.Default)
                    {
                        stored[opt.Id] = value ?? string.Empty;
                    }
                }

                ConfigManager.UpdateCurrentConfig(featureStates, stored);
                SourceGenerator.GenerateFromCurrent();
                AnsiConsole.Clear();
                Log.Information("Saved Current.toml");
                Environment.Exit(0);
                break;
            case Presets:
                Dictionary<string, bool>? newFeatureStates = PresetsPage.Show();
                if (newFeatureStates is not null)
                {
                    featureStates.Clear();
                    foreach ((string? k, bool v) in newFeatureStates)
                    {
                        featureStates.Add(k, v);
                    }
                }
                break;
            case Exit:
                AnsiConsole.Clear();
                Environment.Exit(0);
                break;
        }

        if (groupNames.Contains(selected))
        {
            string toEnter = new([.. groupNames[groupNames.IndexOf(selected)].Skip(4)]);
            GroupNode node;
            if (group is null)
            {
                node = groups.First(n => n.Group.Name == toEnter);
            }
            else
            {
                node = group.Children.First(c => c.Group.Name == toEnter);
            }
            return node;
        }

        if (optionLabels.TryGetValue(selected, out ManifestConfig.OptionConfig? selectedOption))
        {
            PromptOption(selectedOption, optionValues);
            return current;
        }

        if (featureLabels.TryGetValue(selected, out ManifestConfig.FeatureConfig? selectedFeature))
        {
            ToggleFeature(ConfigManager.ManifestConfig!, selectedFeature.Id, featureStates);
            return current;
        }
        return current;
    }

    private static string FeatureLabel(ManifestConfig manifest, ManifestConfig.FeatureConfig feature, Dictionary<string, bool> featureStates)
    {
        bool on = featureStates[feature.Id];
        string box = FeatureDependencies.ChoiceOf(manifest, feature.Id) is null
            ? (on ? "[[*]] " : "[[ ]] ")
            : (on ? "(*) " : "( ) ");
        string label = box + feature.Name;

        if (feature.Depends.Count > 0)
        {
            label += $" [gray](requires: {NamesOf(manifest, feature.Depends)})[/]";
        }

        if (feature.Select.Count > 0)
        {
            label += $" [gray](selects: {NamesOf(manifest, feature.Select)})[/]";
        }

        List<string> requiredBy = FeatureDependencies.RequiredBy(manifest, feature.Id);
        if (requiredBy.Count > 0)
        {
            label += $" [gray](required by: {NamesOf(manifest, requiredBy)})[/]";
        }

        return label;
    }

    private static void ToggleFeature(ManifestConfig manifest, string id, Dictionary<string, bool> featureStates)
    {
        if (featureStates[id])
        {
            List<string> dependants = FeatureDependencies.EnabledDependants(manifest, id, featureStates.Where(kv => kv.Value).Select(kv => kv.Key));

            if (dependants.Count > 0)
            {
                s_notice = $"[yellow]Can't disable {NamesOf(manifest, [id])}: required by {NamesOf(manifest, dependants)}[/]";
                return;
            }

            IEnumerable<string> rest = featureStates.Where(kv => kv.Value && kv.Key != id).Select(kv => kv.Key);

            if (FeatureDependencies.FindProblem(manifest, rest) is { } blocked)
            {
                s_notice = $"[yellow]Can't disable {NamesOf(manifest, [id])}: {blocked}[/]";
                return;
            }

            featureStates[id] = false;
            return;
        }

        List<string> toEnable = [.. FeatureDependencies.WithSelected(manifest, id).Where(d => !featureStates[d])];

        // Enabling a choice member switches off its sibling. Selected members never switch
        ManifestConfig.ChoiceConfig? choice = FeatureDependencies.ChoiceOf(manifest, id);
        List<string> switchedOff = [.. featureStates.Where(kv => kv.Value && choice is not null && choice.Members.Contains(kv.Key)).Select(kv => kv.Key)];
        IEnumerable<string> after = featureStates.Where(kv => kv.Value && !switchedOff.Contains(kv.Key)).Select(kv => kv.Key).Concat(toEnable);

        if (FeatureDependencies.FindProblem(manifest, after) is { } problem)
        {
            s_notice = $"[yellow]Can't enable {NamesOf(manifest, [id])}: {problem}[/]";
            return;
        }

        foreach (string off in switchedOff)
        {
            featureStates[off] = false;
        }

        foreach (string enable in toEnable)
        {
            featureStates[enable] = true;
        }

        List<string> notes = [];

        if (switchedOff.Count > 0)
        {
            notes.Add($"Switched off {NamesOf(manifest, switchedOff)}");
        }

        List<string> selected = [.. toEnable.Where(d => d != id)];
        if (selected.Count > 0)
        {
            notes.Add($"Also enabled {NamesOf(manifest, selected)} (selected by {NamesOf(manifest, [id])})");
        }

        if (notes.Count > 0)
        {
            s_notice = $"[yellow]{string.Join(". ", notes)}[/]";
        }
    }

    // Green when at the default, yellow when changed, grey for null
    private static string OptionLabel(ManifestConfig.OptionConfig option, string? value)
    {
        string name = Markup.Escape(option.Name);

        if (value is null)
        {
            return $"{name}: [gray]null[/]";
        }

        string shown = Markup.Escape(value);

        return value == option.Default
            ? $"{name}: [green]{shown}[/]"
            : $"{name}: [yellow]{shown}[/] [gray](default {Markup.Escape(option.Default ?? "null")})[/]";
    }

    private static void PromptOption(ManifestConfig.OptionConfig option, Dictionary<string, string?> optionValues)
    {
        string? value = optionValues[option.Id];
        string shown = value is null ? "[gray]null[/]" : $"[aqua]{Markup.Escape(value)}[/]";

        string input = AnsiConsole.Prompt(new TextPrompt<string>(
                $"{Markup.Escape(option.Name)} [gray]({option.Type}, default {Markup.Escape(option.Default ?? "null")}, empty for null)[/], current {shown}:")
            .AllowEmpty()
            .Validate(value => FeatureOptions.CheckValue(option, value) is { } problem
                ? ValidationResult.Error(problem)
                : ValidationResult.Success()));

        optionValues[option.Id] = input.Length == 0 ? null : input;
    }

    private static string NamesOf(ManifestConfig manifest, IEnumerable<string> ids)
    {
        return string.Join(", ", ids.Select(id => manifest.Feature.First(f => f.Id == id).Name));
    }

    private static void DrawHeader(string path)
    {
        AnsiConsole.MarkupLine("[aqua]Yukihana YKConfig[/]\n\n");
        AnsiConsole.MarkupLine("[gray]{0}[/]", path);

        if (s_notice is not null)
        {
            AnsiConsole.MarkupLine(s_notice);
            s_notice = null;
        }
    }
}
