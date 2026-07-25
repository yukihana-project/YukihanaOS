// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig;

internal sealed record DependencyNode(
    ManifestConfig.FeatureConfig FeatureConfig,
    string Id,
    string[] DependenciesByIds
);

internal sealed class ResolvedNode
{
    public required ManifestConfig.FeatureConfig FeatureConfig { get; set; }
    public required string Id { get; set; }
    public List<ResolvedNode> Dependencies { get; set; } = [];
}

internal sealed class DependencyGraph
{
    private readonly HashSet<DependencyNode> _visited = [];
    private readonly HashSet<DependencyNode> _visiting = [];

    private DependencyNode[] _nodes = [];
    private Dictionary<string, DependencyNode> _lookup = [];

    public List<ResolvedNode> BuildOrder { get; } = [];

    public void BuildDependencyGraph(ManifestConfig.FeatureConfig[] included)
    {
        _visited.Clear();
        _visiting.Clear();
        BuildOrder.Clear();

        _nodes = [.. included.Select(i =>
            new DependencyNode(i, i.Id, [.. i.Depends]))];

        _lookup = _nodes.ToDictionary(n => n.Id);

        foreach (DependencyNode node in _nodes)
        {
            if (!_visited.Contains(node))
            {
                BuildOrder.Add(DFS(node));
            }
        }
    }

    private ResolvedNode DFS(DependencyNode current)
    {
        if (_visiting.Contains(current))
        {
            throw new InvalidOperationException(
                $"Cycle detected involving '{current.Id}'.");
        }

        if (_visited.Contains(current))
        {
            throw new InvalidOperationException(
                $"'{current.Id}' has already been resolved.");
        }

        _visiting.Add(current);

        ResolvedNode resolved = new()
        {
            FeatureConfig = current.FeatureConfig,
            Id = current.Id
        };

        foreach (string dependencyId in current.DependenciesByIds)
        {
            if (!_lookup.TryGetValue(dependencyId, out DependencyNode? dependency))
            {
                throw new InvalidOperationException(
                    $"Unknown dependency '{dependencyId}' required by '{current.Id}'.");
            }

            ResolvedNode dependencyNode = DFS(dependency);
            resolved.Dependencies.Add(dependencyNode);
        }

        _visiting.Remove(current);
        _visited.Add(current);

        return resolved;
    }
}
