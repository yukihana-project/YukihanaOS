// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Serilog;
using Spectre.Console;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig.Menu;

// imma cry T_T
internal static class ConfigPage
{
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
        string[] featureNames = [.. features
            .Where(f => featureStates.ContainsKey(f.Id))
            .Select(f => featureStates[f.Id] ? "[[*]] " + f.Name : "[[ ]] " + f.Name)];

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

        if (featureNames.Contains(selected))
        {
            string featName = new([.. featureNames[featureNames.IndexOf(selected)].Skip(6)]);
            ManifestConfig.FeatureConfig feat = features.First(f => f.Name == featName);
            featureStates[feat.Id] = !featureStates[feat.Id];
            return current;
        }
        return current;
    }

    private static void DrawHeader(string path)
    {
        AnsiConsole.MarkupLine("[aqua]Yukihana YKConfig[/]\n\n");
        AnsiConsole.MarkupLine("[gray]{0}[/]", path);
    }
}
