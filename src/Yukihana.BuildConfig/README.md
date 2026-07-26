# YKConfig

> A Kconfig-inspired configuration system for C# MSBuild projects.

## About

**YKConfig** aims to provide versatile feature configuration of "features" inside a C# project. It allows you to exclude sources of disabled features, use preprocessor `#define` and use normal C# if statements through a generated static class.

---

## Using it

There are several steps required to use YKConfig.

### Configuring it

First, once the sources are in your project directory, edit `Yukihana.BuildConfig.csproj`. Locate the following line:

```xml
    <!-- Edit this to where your porject is -->
    <YukihanaDir>$(MSBuildProjectDirectory)\..\Yukihana\</YukihanaDir>
```

This path is where the executable will be placed after it is built and published

> [!NOTE]
> You don't need to specify configuration. The tool will automatically set it to Release, and publish it as a NativeAOT single-file executable.

After that, you need to edit `Configuration.cs` file. Here is snippet

```cs
public static class Configuration
{
    /// <summary>
    /// Directory where the tool stores all of its files.
    /// </summary>
    public static readonly string ToolFolder = "./Build/";

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
}

```

### Configuring your project

Now, after you've configured the tool, you need to configure multiple files, so tool understands what to do.

Run:

```bash
./ykconfig init
```

This command creates the required directories and files. I will use default tool configuration to explain everything next.

You should see `Build` folder with `Manifest.toml` inside. Here is snippet of how to configure it 

```toml
# This version should be 1
version = 1

# Metadata defines your project, like name and description 
[metadata]
name = "Yukihana OS"
description = "Feature configuration manifest"

# Groups are used for menu configuration as "folders" where features are placed
# I haven't tested if it will work without groups
[[group]]
id = "kernel"
name = "Kernel"

[[group]]
id = "debug"
name = "Debug"

# Here, group can be nested under another group.
[[group]]
id = "debug.logger"
name = "Logger"
parent = "debug"


# Features require the id field to use snake_case, while everything else does not require it
# "name" field is display name used in menu configuration
[[feature]]
id = "init_ram_fs"
name = "InitRamFS"
group = "kernel"

description = "Allows kernel to load temporary filesystem from archive."

# This is preprocessor define to add when feature is enabled
define = "FEATURE_INIT_RAM_FS"

# This is also used for menu configuration, to automatically set or unset features
# This is optional field
enabled_by_default = true

[[feature]]
id = "sinks_serial"
name = "Serial Sink"
group = "debug.logger"

description = "Enables kernel to log to serial port"

define = "FEATURE_SERIAL_SINK"

# This list contains sources to exclude when feature is disabled
exclude = [
    "Debug/Sinks/ConsoleSink.cs"
]
```

Also, in `Configs` folder you can define presets. File names are used as preset names. Here is a snippet

```toml
description = "Maximal configuration"

# Here you enumerate features using their ids defined in Manifest.toml to enclude
enabled = [
    "init_ram_fs",
    "sinks_serial"
]
```

For configuring your project this is it. Don't forget to include generated `.targets` file in your build (via `.csproj` or `Directory.Build.targets`)

> [!NOTE]
> Do not place anything inside `Generated` folder, as during the `clean` command, its contents will be deleted