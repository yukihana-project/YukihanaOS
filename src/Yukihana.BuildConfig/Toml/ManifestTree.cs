// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.BuildConfig.Toml;

internal sealed class ManifestTree
{
    public List<GroupNode> RootGroups { get; } = [];

    public GroupNode? GetParent(GroupNode node)
    {
        // Traverse from root to find parent
        return FindParent(RootGroups, node);
    }

    private GroupNode? FindParent(IEnumerable<GroupNode> groups, GroupNode target)
    {
        foreach (var g in groups)
        {
            if (g.Children.Contains(target))
                return g;

            var parent = FindParent(g.Children, target);
            if (parent != null)
                return parent;
        }
        return null;
    }

    public string GetNamePath(GroupNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!TryGetPath(RootGroups, node, static n => n.Group.Name, out var path))
            throw new InvalidOperationException("Node does not belong to the tree.");

        return string.Join(" -> ", path);
    }

    public string GetIdPath(GroupNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!TryGetPath(RootGroups, node, static n => n.Group.Id, out var path))
            throw new InvalidOperationException("Node does not belong to the tree.");

        return string.Join("/", path);
    }

    public GroupNode? GetNodeByIdPath(string idPath)
    {

        if (string.IsNullOrWhiteSpace(idPath))
            return null;

        var parts = idPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        List<GroupNode> currentLevel = RootGroups;
        GroupNode? current = null;

        foreach (var id in parts)
        {
            current = currentLevel.FirstOrDefault(x => x.Group.Id == id);

            if (current is null)
                return null;

            currentLevel = current.Children;
        }

        return current;
    }

    private static bool TryGetPath(
        IEnumerable<GroupNode> nodes,
        GroupNode target,
        Func<GroupNode, string> selector,
        out List<string> path)
    {
        foreach (var node in nodes)
        {
            path = [];

            if (TryGetPath(node, target, selector, path))
                return true;
        }

        path = [];
        return false;
    }

    private static bool TryGetPath(
        GroupNode current,
        GroupNode target,
        Func<GroupNode, string> selector,
        List<string> path)
    {
        path.Add(selector(current));

        if (ReferenceEquals(current, target))
            return true;

        foreach (var child in current.Children)
        {
            if (TryGetPath(child, target, selector, path))
                return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    public Dictionary<string, bool> GetFeatureStates()
    {

        var features = EnumerateFeatures(this)
            .ToDictionary(f => f.Id);

        var result = new Dictionary<string, bool>();
        var visiting = new HashSet<string>();

        foreach (var feature in features.Values)
            Evaluate(feature.Id);

        return result;

        bool Evaluate(string id)
        {
            if (result.TryGetValue(id, out var enabled))
                return enabled;

            if (!features.TryGetValue(id, out var feature))
                return false;

            if (!visiting.Add(id))
                throw new InvalidOperationException($"Circular dependency detected involving '{id}'.");

            enabled = feature.EnabledByDefault ?? false;

            if (enabled)
            {
                // A feature is disabled if any dependency is disabled.
                foreach (var dependency in feature.Depends)
                {
                    if (!Evaluate(dependency))
                    {
                        enabled = false;
                        break;
                    }
                }
            }

            if (enabled)
            {
                // A feature is disabled if any excluded feature is enabled.
                foreach (var excluded in feature.Exclude)
                {
                    if (Evaluate(excluded))
                    {
                        enabled = false;
                        break;
                    }
                }
            }

            visiting.Remove(id);
            result[id] = enabled;
            return enabled;
        }
    }

    private static IEnumerable<ManifestConfig.FeatureConfig> EnumerateFeatures(ManifestTree tree)
    {
        foreach (var root in tree.RootGroups)
        {
            foreach (var feature in EnumerateFeatures(root))
                yield return feature;
        }
    }

    private static IEnumerable<ManifestConfig.FeatureConfig> EnumerateFeatures(GroupNode node)
    {
        foreach (var feature in node.Features)
            yield return feature;

        foreach (var child in node.Children)
        {
            foreach (var feature in EnumerateFeatures(child))
                yield return feature;
        }
    }
}

internal sealed class GroupNode
{
    public required ManifestConfig.GroupConfig Group { get; init; }
    public List<GroupNode> Children { get; } = [];
    public List<ManifestConfig.FeatureConfig> Features { get; } = [];
}

internal static class ManifestTreeBuilder
{
    public static ManifestTree Build(ManifestConfig manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        ManifestTree tree = new();

        var groups = manifest.Group.ToDictionary(
            g => g.Id,
            g => new GroupNode
            {
                Group = g
            });
        
        foreach (GroupNode? node in groups.Values)
        {
            if (string.IsNullOrWhiteSpace(node.Group.Parent))
            {
                tree.RootGroups.Add(node);
                continue;
            }

            if (!groups.TryGetValue(node.Group.Parent, out GroupNode? parent))
            {
                throw new InvalidOperationException(
                    $"Group '{node.Group.Id}' references unknown parent '{node.Group.Parent}'");
            }

            parent.Children.Add(node);
        }

        foreach (ManifestConfig.FeatureConfig feature in manifest.Feature)
        {
            if (!groups.TryGetValue(feature.Group, out GroupNode? group))
            {
                throw new InvalidOperationException(
                    $"Feature '{feature.Id}' references unknown group '{feature.Group}'.");
            }

            group.Features.Add(feature);
        }

        return tree;
    }
}