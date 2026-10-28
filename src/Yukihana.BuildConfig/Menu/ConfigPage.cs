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

        ManifestTree tree = ManifestTreeBuilder.Build(ConfigManager.ManifestConfig!);
        Dictionary<string, bool> featureStates = tree.GetFeatureStates();

        GroupNode? node = null;

        while (true)
        {
            node = Process(tree, node, featureStates); ;
        }
    }

    private static GroupNode? Process(ManifestTree tree, GroupNode? current, Dictionary<string, bool> featureStates)
    {
        AnsiConsole.Clear();

        string pathNames = "Root";
        string path = string.Empty;
        GroupNode? group = null;

        List<GroupNode> groups = tree.RootGroups;
        List<ManifestConfig.FeatureConfig> features = [];

        if (current is not null)
        {
            pathNames = "Root -> " + tree.GetNamePath(current);
            group = tree.GetNodeByIdPath(tree.GetIdPath(current));

            groups = group!.Children;
            features = group.Features;
        }

        DrawHeader(pathNames);

        string[] groupNames = [.. groups.Select(g => "    " + g.Group.Name)];
        Dictionary<string, ManifestConfig.FeatureConfig> featureLabels = [];
        foreach (ManifestConfig.FeatureConfig feat in features.Where(f => featureStates.ContainsKey(f.Id)))
        {
            featureLabels[FeatureLabel(ConfigManager.ManifestConfig!, feat, featureStates)] = feat;
        }

        string[] featureNames = [.. featureLabels.Keys];

        const string Back = "[gray]<Back>[/]";
        const string Save = "[gray]<Save>[/]";
        const string Presets = "[gray]<Presets>[/]";
        const string Exit = "[gray]<Exit>[/]";

        string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("YKConfig selection")
            .EnableSearch()
            .PageSize(15)
            .MoreChoicesText("[gray](More items available)[/]")
            .AddChoiceGroup("Config", [.. groupNames, .. featureNames])
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
                ConfigManager.UpdateCurrentConfig(featureStates);
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

        if (featureLabels.TryGetValue(selected, out ManifestConfig.FeatureConfig? selectedFeature))
        {
            ToggleFeature(ConfigManager.ManifestConfig!, selectedFeature.Id, featureStates);
            return current;
        }
        return current;
    }

    private static string FeatureLabel(ManifestConfig manifest, ManifestConfig.FeatureConfig feature, Dictionary<string, bool> featureStates)
    {
        string label = (featureStates[feature.Id] ? "[[*]] " : "[[ ]] ") + feature.Name;

        if (feature.Depends.Count > 0)
        {
            label += $" [gray](requires: {NamesOf(manifest, feature.Depends)})[/]";
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

            featureStates[id] = false;
            return;
        }

        List<string> toEnable = [.. FeatureDependencies.WithRequirements(manifest, id).Where(d => !featureStates[d])];

        foreach (string enable in toEnable)
        {
            featureStates[enable] = true;
        }

        List<string> required = [.. toEnable.Where(d => d != id)];
        if (required.Count > 0)
        {
            s_notice = $"[yellow]Also enabled {NamesOf(manifest, required)} (required by {NamesOf(manifest, [id])})[/]";
        }
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
