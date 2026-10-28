// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Yukihana.BuildConfig.Toml;

internal sealed class ManifestConfig
{
    public int Version { get; set; } = 1;

    public MetadataConfig Metadata { get; set; } = new();

    public List<GroupConfig> Group { get; set; } = [];

    public List<FeatureConfig> Feature { get; set; } = [];

    public List<ChoiceConfig> Choice { get; set; } = [];

    public List<OptionConfig> Option { get; set; } = [];

    internal sealed class MetadataConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    internal sealed class GroupConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
    }

    internal sealed class OptionConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // "string" or "int"
        public string Type { get; set; } = "string";

        // Stored as text, so it is written as a TOML string
        public string? Default { get; set; }
    }

    internal sealed class ChoiceConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Members { get; set; } = [];
        public bool Optional { get; set; }
        public string? Default { get; set; }
    }

    internal sealed class FeatureConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Define { get; set; } = string.Empty;
        public bool? EnabledByDefault { get; set; }
        public List<string> Depends { get; set; } = [];
        public List<string> Select { get; set; } = [];
        public List<string> Exclude { get; set; } = [];
    }
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace
)]
[TomlSerializable(typeof(ManifestConfig))]
[TomlSerializable(typeof(ManifestConfig.FeatureConfig))]
[TomlSerializable(typeof(ManifestConfig.GroupConfig))]
[TomlSerializable(typeof(ManifestConfig.ChoiceConfig))]
[TomlSerializable(typeof(ManifestConfig.OptionConfig))]
[TomlSerializable(typeof(ManifestConfig.MetadataConfig))]
internal partial class ManifestConfigContext : TomlSerializerContext
{
}
