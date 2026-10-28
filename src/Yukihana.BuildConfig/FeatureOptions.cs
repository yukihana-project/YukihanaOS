// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig;

internal static class FeatureOptions
{
    /// <summary>
    /// Value in effect: the stored value, else the default. An empty stored value means null.
    /// </summary>
    public static string? Effective(ManifestConfig.OptionConfig option, Dictionary<string, string>? values)
    {
        if (values is not null && values.TryGetValue(option.Id, out string? stored))
        {
            return stored.Length == 0 ? null : stored;
        }

        return option.Default;
    }

    /// <summary>
    /// Problem with a value for the option's type, or null if it fits. Empty is always allowed and means null.
    /// </summary>
    public static string? CheckValue(ManifestConfig.OptionConfig option, string value)
    {
        if (value.Length == 0)
        {
            return null;
        }

        return option.Type switch
        {
            "string" => null,
            "int" when int.TryParse(value, out _) => null,
            "int" => $"'{value}' is not an int",
            _ => $"unknown type '{option.Type}'"
        };
    }

    /// <summary>
    /// First problem with stored values: an undeclared option, or a value that does not fit its type.
    /// </summary>
    public static string? FindProblem(ManifestConfig manifest, Dictionary<string, string>? values)
    {
        if (values is null)
        {
            return null;
        }

        foreach ((string id, string value) in values)
        {
            ManifestConfig.OptionConfig? option = manifest.Option.FirstOrDefault(o => o.Id == id);

            if (option is null)
            {
                return $"'{id}' is not a declared option";
            }

            if (CheckValue(option, value) is { } problem)
            {
                return $"option '{id}': {problem}";
            }
        }

        return null;
    }

    /// <summary>
    /// C# type of the generated constant.
    /// </summary>
    public static string CSharpType(ManifestConfig.OptionConfig option)
    {
        return option.Type == "int" ? "int?" : "string?";
    }

    /// <summary>
    /// C# literal for a value that already passed <see cref="CheckValue"/>.
    /// </summary>
    public static string Literal(ManifestConfig.OptionConfig option, string? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (option.Type == "int")
        {
            return int.Parse(value).ToString();
        }

        string escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        return $"\"{escaped}\"";
    }
}
