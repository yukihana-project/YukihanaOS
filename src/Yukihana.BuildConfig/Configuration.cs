// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.BuildConfig;

/// <summary>
/// This class hold tool's configuration like paths
/// Edit this, to change some behaviour to what suits you
/// </summary>
public static class Configuration
{
    /// <summary>
    /// Path, where tool will store all of its files
    /// </summary>
    public static readonly string ToolFolder = "./Build/";

    /// <summary>
    /// Directory, where presets (combination of features) are defined
    /// </summary>
    public static string ConfigsDirectoryPath => Path.Combine(ToolFolder, "Configs/");

    /// <summary>
    /// Output directory where generated files will be placed
    /// </summary>
    public static string OutputDirectoryPath => Path.Combine(ToolFolder, "Generated/");

    /// <summary>
    /// File path where tool's manifest is located
    /// </summary>
    public static string ManifestTomlPath => Path.Combine(ToolFolder, "Manifest.toml");

    /// <summary>
    /// Path to file where selected features are stored
    /// </summary>
    public static string CurrentTomlPath => Path.Combine(ToolFolder, "Current.toml");

    /// <summary>
    /// Path to file where generated state is stored
    /// </summary>
    public static string StateTomlPath => Path.Combine(ToolFolder, "State.toml");

    #region .cs file generation

    /// <summary>
    /// Header of the generated C# file
    /// </summary>
    public static string[] GeneratedCsHeader => [
        "Yukihana OS 2026 Yukihana OS Contributors",
        "Licensed under the Apache License, Version 2.0. See LICENSE for details.",
        "This is auto-generated file. DO NOT EDIT"];

    /// <summary>
    /// Namespace of the generated C# file to use
    /// </summary>
    public static string GeneratedCsNamespace => "Yukihana";

    /// <summary>
    /// Name of the static class where all boolean switches for features are stored
    /// </summary>
    public static string GeneratedCsClassName => "Features";

    /// <summary>
    /// Path to file where generate C# file will be stored
    /// </summary>
    public static string GeneratedCsFilePath => Path.Combine(OutputDirectoryPath, "Features.g.cs");
    #endregion

    /// <summary>
    /// Path to generated .targets file that manages build (Must be included into build via tag)
    /// </summary>
    public static string GeneratedTargetsFilePath => Path.Combine(OutputDirectoryPath, "Features.g.targets");
}
