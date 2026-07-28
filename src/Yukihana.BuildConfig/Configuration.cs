// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.BuildConfig;

/// <summary>
/// This class holds tool's configuration like paths
/// Edit this, to change some behaviour to what suits you
/// </summary>
public static class Configuration
{
    public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Directory where the tool stores all of its files.
    /// </summary>
    public static readonly string ToolFolder = Path.Combine(BaseDirectory, "Build/");

    /// <summary>
    /// Directory, where presets (combination of features) are defined.
    /// </summary>
    public static string ConfigsDirectoryPath => Path.Combine(ToolFolder, "Configs/");

    /// <summary>
    /// Output directory for generated files.
    /// </summary>
    public static string OutputDirectoryPath => Path.Combine(ToolFolder, "Generated/");

    /// <summary>
    /// File path where tool's manifest is located.
    /// </summary>
    public static string ManifestTomlPath => Path.Combine(ToolFolder, "Manifest.toml");

    /// <summary>
    /// Path to the file storing the selected features.
    /// </summary>
    public static string CurrentTomlPath => Path.Combine(ToolFolder, "Current.toml");

    /// <summary>
    /// Path to file where generated state is stored.
    /// </summary>
    public static string StateTomlPath => Path.Combine(ToolFolder, "State.toml");

    #region .cs file generation

    /// <summary>
    /// Header of the generated C# file.
    /// </summary>
    public static string[] GeneratedCsHeader => [
        "Yukihana OS 2026 Yukihana OS Contributors",
        "Licensed under the Apache License, Version 2.0. See LICENSE for details.",
        "This is auto-generated file. DO NOT EDIT"];

    /// <summary>
    /// Namespace of the generated C# file to use.
    /// </summary>
    public static string GeneratedCsNamespace => "Yukihana";

    /// <summary>
    /// Name of the static class where all boolean switches for features are stored.
    /// </summary>
    public static string GeneratedCsClassName => "Features";

    /// <summary>
    /// Path to the generated C# file.
    /// </summary>
    public static string GeneratedCsFilePath => Path.Combine(OutputDirectoryPath, "Features.g.cs");
    #endregion

    /// <summary>
    /// Path to generated .targets file that manages build (Must be included into build via tag).
    /// </summary>
    public static string GeneratedTargetsFilePath => Path.Combine(OutputDirectoryPath, "Features.g.targets");

    /// <summary>
    /// Path to the source initramfs folders. Requires <see cref="Features.BuildInitRamFs"/> to be true
    /// </summary>
    public static string InitRamFsSourcePath => Path.Combine(ToolFolder, "InitRamFs/");

    /// <summary>
    /// Path to the default initramfs source. Requires <see cref="Features.BuildInitRamFs"/> to be true
    /// </summary>
    public static string DefaultInitRamFsPath => Path.Combine(InitRamFsSourcePath, "default/");

    /// <summary>
    /// Path to the local initramfs source. If exists, and has contents, this will be used instead of default one.
    /// Requires <see cref="Features.BuildInitRamFs"/> to be true
    /// </summary>
    public static string LocalInitRamFsPath => Path.Combine(InitRamFsSourcePath, "local/");

    /// <summary>
    /// Path where generated initramfs will be stored. Requires <see cref="Features.BuildInitRamFs"/> to be true
    /// </summary>
    public static string GeneratedInitRamFsPath => Path.Combine(OutputDirectoryPath, "initramfs.cpio.gz");

    /// <summary>
    /// Logical name for generated initramfs to use with GetManifestResourceStream
    /// Requires <see cref="Features.BuildInitRamFs"/> to be true
    /// </summary>
    public const string InitRamFsLogicalName = "Yukihana.initramfs.cpio.gz";

    /// <summary>
    /// YKConfig specific feature switches
    /// </summary>
    public static class Features
    {
        /// <summary>
        /// Adds target to build initram cpio.gz archive from default directory,
        /// or from local directory if present
        /// </summary>
        public const bool BuildInitRamFs = true;
    }
}
