// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using FirmwareKit.CPIO;
using FirmwareKit.CPIO.Model;
using Serilog;
using Tomlyn;
using Yukihana.BuildConfig.SourceGenerators;
using Yukihana.BuildConfig.Toml;

namespace Yukihana.BuildConfig;

internal static class SourceGenerator
{
    private const string INCLUDE_ITEM_TEMPLATE = "_YKConfig_IncludeFeature_{0}";


    private const string INTERNAL_RAMFS_TARGET = "_YKConfig_Internal_IncludeInitRamFs";
    private const string INIT_RAM_FS_FEATURE_ID = "init_ram_fs";

    public static void GenerateFromCurrent()
    {
        ConfigManager.LoadConfigs();

        // Missing on a fresh checkout until init runs; the archive and sources are written here
        Directory.CreateDirectory(Configuration.OutputDirectoryPath);

        ManifestConfig manifest = ConfigManager.ManifestConfig!;
        CurrentConfig current = ConfigManager.CurrentConfig!;

        HashSet<ResolvedNode> allFeatures = ResolveFeatures(manifest, current);

        if (allFeatures.Any(n => n.Id == INIT_RAM_FS_FEATURE_ID))
        {
            BuildRamFs();
        }

        string outputPath = Configuration.GeneratedCsFilePath;

        Log.Verbose("Generating class");

        using (FileStream csFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            string generatedClass = GenerateFeatureConstants(allFeatures, manifest, current);
            csFs.Write(Encoding.UTF8.GetBytes(generatedClass));
        }

        outputPath = Configuration.GeneratedTargetsFilePath;

        Log.Verbose("Generating targets file");

        using (FileStream targetsFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            string generatedTargets = GenerateTargetsFile(allFeatures, manifest);
            targetsFs.Write(Encoding.UTF8.GetBytes(generatedTargets));
        }

        WriteState();
    }

    /// <summary>
    /// Writes State.toml with hashes of the current Manifest.toml and Current.toml.
    /// Does not touch generated sources, so callers must make sure they match first.
    /// </summary>
    public static void WriteState()
    {
        CurrentConfig current = ConfigManager.CurrentConfig!;

        Log.Verbose("Creating State file");

        using SHA256 sha256 = SHA256.Create();
        using FileStream manifestStream = File.OpenRead(Configuration.ManifestTomlPath);
        using FileStream currentStream = File.OpenRead(Configuration.CurrentTomlPath);

        ConfigManager.StateConfig = new()
        {
            GeneratorVersion = typeof(Program).Assembly.GetName().Version ?? new(1, 0, 0),
            ManifestHash = Convert.ToHexStringLower(sha256.ComputeHash(manifestStream)),
            ConfigurationHash = Convert.ToHexStringLower(sha256.ComputeHash(currentStream)),
            GeneratedTime = DateTime.UtcNow
        };

        if (current.Config is not null)
        {
            if (File.Exists(Path.Combine(Configuration.ConfigsDirectoryPath, $"{current.Config}.toml")))
            {
                ConfigManager.StateConfig.Preset = current.Config;
            }
        }

        using FileStream stateStream = File.Open(Configuration.StateTomlPath, FileMode.Create, FileAccess.Write, FileShare.None);
        TomlSerializer.Serialize(stateStream, ConfigManager.StateConfig, StateConfigContext.Default);
    }

    /// <summary>
    /// True when Features.g.cs and Features.g.targets already hold what Current.toml would generate.
    /// Writes nothing.
    /// </summary>
    public static bool GeneratedFilesMatchCurrent()
    {
        ConfigManager.LoadConfigs();

        ManifestConfig manifest = ConfigManager.ManifestConfig!;
        HashSet<ResolvedNode> allFeatures = ResolveFeatures(manifest, ConfigManager.CurrentConfig!);

        return FileMatches(Configuration.GeneratedCsFilePath, GenerateFeatureConstants(allFeatures, manifest, ConfigManager.CurrentConfig!))
            && FileMatches(Configuration.GeneratedTargetsFilePath, GenerateTargetsFile(allFeatures, manifest));
    }

    private static bool FileMatches(string path, string expected)
    {
        return File.Exists(path) && File.ReadAllText(path) == expected;
    }

    private static HashSet<ResolvedNode> ResolveFeatures(ManifestConfig manifest, CurrentConfig current)
    {
        if (FeatureOptions.FindProblem(manifest, current.Options) is { } problem)
        {
            Log.Fatal("Current.toml is invalid: {Problem}", problem);
            Environment.Exit(1);
        }

        Log.Verbose("Building dependency graph");
        return Flatten(BuildResolvedGraph(manifest, current));
    }

    private static List<ResolvedNode> BuildResolvedGraph(
        ManifestConfig manifest,
        CurrentConfig current)
    {
        current.Enabled ??= [];

        ManifestConfig.FeatureConfig[] included =
        [
            .. manifest.Feature.Where(f => current.Enabled.Contains(f.Id))
        ];

        DependencyGraph graph = new();
        graph.BuildDependencyGraph(included);

        return graph.BuildOrder;
    }

    public static HashSet<ResolvedNode> Flatten(IEnumerable<ResolvedNode> roots)
    {
        HashSet<ResolvedNode> result = [];
        HashSet<ResolvedNode> visited = [];

        foreach (ResolvedNode root in roots)
        {
            Visit(root, visited, result);
        }

        return result;
    }

    private static void Visit(
        ResolvedNode node,
        HashSet<ResolvedNode> visited,
        HashSet<ResolvedNode> result)
    {
        if (!visited.Add(node))
        {
            return;
        }

        result.Add(node);

        foreach (ResolvedNode dependency in node.Dependencies)
        {
            Visit(dependency, visited, result);
        }
    }

    private static string ToPascalCase(string input)
    {
        StringBuilder resultBuilder = new();

        foreach (char c in input)
        {
            // Replace anything, but letters and digits, with space
            if (!char.IsLetterOrDigit(c))
            {
                resultBuilder.Append(' ');
            }
            else
            {
                resultBuilder.Append(c);
            }
        }

        string result = resultBuilder.ToString();

        // Make result string all lowercase, because ToTitleCase does not change all uppercase correctly
        result = result.ToLower();

        // Creates a TextInfo based on the "en-US" culture.
        TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

        result = myTI.ToTitleCase(result).Replace(" ", string.Empty);
        return result;
    }

    private static string GenerateFeatureConstants(HashSet<ResolvedNode> graph, ManifestConfig manifest, CurrentConfig current)
    {
        CSharpFileGenerator generator = CSharpFileGenerator.Create();

        generator.Comments(Configuration.GeneratedCsHeader);
        generator.Namespace(Configuration.GeneratedCsNamespace);

        ClassBuilder builder = generator.Class(Configuration.GeneratedCsClassName).Public().Static();

        HashSet<string> enabledFeatures = graph
            .Select(rn => rn.Id)
            .ToHashSet(StringComparer.Ordinal);


        foreach (ManifestConfig.FeatureConfig node in manifest.Feature)
        {
            builder.ConstBool(ToPascalCase(node.Id), enabledFeatures.Contains(node.Id));
        }

        if (manifest.Option.Count > 0)
        {
            ClassBuilder options = generator.Class("Options").Public().Static();

            foreach (ManifestConfig.OptionConfig option in manifest.Option)
            {
                string? value = FeatureOptions.Effective(option, current.Options);
                options.ConstLiteral(FeatureOptions.CSharpType(option), ToPascalCase(option.Id), FeatureOptions.Literal(option, value));
            }
        }

        return generator.Generate();
    }

    private static string GenerateTargetsFile(HashSet<ResolvedNode> graph, ManifestConfig manifest)
    {
        TargetsFileGenerator generator = TargetsFileGenerator.Create();

        const string TARGET_BEFORE = "CoreCompile";

        Log.Verbose("Generating internal targets");

        var enabledIds = graph
            .Select(rn => rn.Id)
            .ToHashSet(StringComparer.Ordinal);

        // Project-relative paths keep the generated file valid when the checkout moves.
        // Feature sources are opt-in: removed from the default glob, added back only when enabled.
        // The exact Features.g.cs is removed too, since a wildcard does not reach it
        string generatedCs = ProjectRelativePath(Configuration.GeneratedCsFilePath);

        generator.AddItemGroup()
            .ExcludeCompile([generatedCs, .. manifest.Feature.SelectMany(f => f.Sources)])
            .IncludeCompile([generatedCs, .. manifest.Feature.Where(f => enabledIds.Contains(f.Id)).SelectMany(f => f.Sources)]);

        if (enabledIds.Contains(INIT_RAM_FS_FEATURE_ID) && HasInitRamFsSource())
        {
            generator.AddTarget(INTERNAL_RAMFS_TARGET)
                .Before("PrepareForBuild")
                .Message("Including initramfs.cpio.gz")
                .EmbedResource(Configuration.GeneratedInitRamFsPath, Configuration.InitRamFsLogicalName, $"Exists('{Configuration.GeneratedInitRamFsPath}')");
        }

        var included = manifest.Feature.Where(f => enabledIds.Contains(f.Id));

        Log.Verbose("Generating include targets");

        foreach (ManifestConfig.FeatureConfig node in included)
        {
            generator.AddTarget(string.Format(INCLUDE_ITEM_TEMPLATE, ToPascalCase(node.Id)))
                .Before(TARGET_BEFORE)
                .Message($"Including feature '{node.Id}'")
                .DefineConstants(node.Define);

            Log.Verbose("Added {FeatureId} feature target as INCLUDED", node.Id);
        }

        return generator.Generate();
    }

    private static string ProjectRelativePath(string path)
    {
        return Path.GetRelativePath(Configuration.BaseDirectory, path).Replace('\\', '/');
    }

    private static bool HasInitRamFsSource()
    {
        return Directory.Exists(Configuration.DefaultInitRamFsPath) || Directory.Exists(Configuration.LocalInitRamFsPath);
    }

    private static void BuildRamFs()
    {
        if (!HasInitRamFsSource())
        {
            Log.Warning("Unable to create initramfs as no directories exists. Run init command.");
            return;
        }

        // Get directory to build

        string defaultDirPath = Configuration.DefaultInitRamFsPath;
        string localDirPath = Configuration.LocalInitRamFsPath;

        string targetInitPath = defaultDirPath;

        if (Directory.Exists(localDirPath) && Directory.GetFileSystemEntries(localDirPath).Length > 0)
        {
            targetInitPath = localDirPath;
        }

        // Make initramfs.cpio.gz

        string root = targetInitPath;

        var entries = new List<ArchiveEntry>();

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(f => f))
        {
            string name = Path.GetRelativePath(root, file)
                            .Replace('\\', '/');

            Log.Verbose("Adding directory {CpioFile} to initramfs", name);

            entries.Add(new ArchiveEntry
            {
                Name = name,
                Data = File.ReadAllBytes(file),
                Metadata = new ArchiveEntryMetadata
                {
                    FileType = CpioFileType.Regular,
                    UnixPermissions = 0x1a4, // 0644 | rw-r--r--
                    LinkCount = 1,
                    ModificationTimeUnixSeconds =
                        new DateTimeOffset(File.GetLastWriteTimeUtc(file))
                            .ToUnixTimeSeconds(),
                }
            });
        }

        var archive = new CpioArchive();

        using (FileStream fs = File.Create(Path.Combine(Configuration.OutputDirectoryPath, "initramfs.cpio")))
        {
            archive.SaveAll(fs, entries, ArchiveFormat.NewAscii);
        }

        try
        {
            Log.Verbose("Creating archive at {ArchiveOutputPath}", Configuration.GeneratedInitRamFsPath);
            using FileStream sourceStream = File.OpenRead(Path.Combine(Configuration.OutputDirectoryPath, "initramfs.cpio"));
            using FileStream archiveStream = File.Create(Configuration.GeneratedInitRamFsPath);
            using GZipStream gZip = new(archiveStream, CompressionLevel.Optimal);

            sourceStream.CopyTo(gZip);
        }
        finally
        {
            File.Delete(Path.Combine(Configuration.OutputDirectoryPath, "initramfs.cpio"));
        }
    }
}
