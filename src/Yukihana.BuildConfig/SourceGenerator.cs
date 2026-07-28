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
    private const string INTERNAL_EXCLUDE_TARGET = "_YKConfig_Internal_ExcludeGeneratedFiles";
    private const string INTERNAL_INCLUDE_SOURCE_TARGET = "_YKConfig_Internal_IncludeGeneratedSource";
    private const string EXCLUDE_ITEM_TEMPLATE = "_YKConfig_ExcludeFeature_{0}";
    private const string INCLUDE_ITEM_TEMPLATE = "_YKConfig_IncludeFeature_{0}";


    private const string INTERNAL_RAMFS_TARGET = "_YKConfig_Internal_IncludeInitRamFs";

    public static void GenerateFromCurrent()
    {
        ConfigManager.LoadConfigs();

        ManifestConfig manifest = ConfigManager.ManifestConfig!;
        CurrentConfig current = ConfigManager.CurrentConfig!;

        Log.Verbose("Building dependency graph");

        List<ResolvedNode> graph = BuildResolvedGraph(manifest, current);
        HashSet<ResolvedNode> allFeatures = Flatten(graph);

        string outputPath = Configuration.GeneratedCsFilePath;

        Log.Verbose("Generating class");

        using (FileStream csFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            string generatedClass = GenerateFeatureConstants(allFeatures, manifest);
            csFs.Write(Encoding.UTF8.GetBytes(generatedClass));
        }

        outputPath = Configuration.GeneratedTargetsFilePath;

        Log.Verbose("Generating targets file");

        using (FileStream targetsFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            string generatedTargets = GenerateTargetsFile(allFeatures, manifest);
            targetsFs.Write(Encoding.UTF8.GetBytes(generatedTargets));
        }

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

        using FileStream stateStream = File.OpenWrite(Configuration.StateTomlPath);
        TomlSerializer.Serialize(stateStream, ConfigManager.StateConfig, StateConfigContext.Default);
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

    private static string GenerateFeatureConstants(HashSet<ResolvedNode> graph, ManifestConfig manifest)
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

        return generator.Generate();
    }

    private static string GenerateTargetsFile(HashSet<ResolvedNode> graph, ManifestConfig manifest)
    {
        TargetsFileGenerator generator = TargetsFileGenerator.Create();

        const string TARGET_BEFORE = "CoreCompile";

        Log.Verbose("Generating internal targets");

        generator.AddTarget(INTERNAL_EXCLUDE_TARGET)
            .Before(TARGET_BEFORE)
            .Message("Excluding Generated directory", TargetsFileGenerator.Importance.Low)
            .ExcludeCompile(Path.Join(Configuration.OutputDirectoryPath, "*"));

        generator.AddTarget(INTERNAL_INCLUDE_SOURCE_TARGET)
            .Before(TARGET_BEFORE)
            .After(INTERNAL_EXCLUDE_TARGET)
            .Message("Including Features.g.cs", TargetsFileGenerator.Importance.Low)
            .IncludeCompile(Configuration.GeneratedCsFilePath);

        if (Configuration.Features.BuildInitRamFs)
        {
            BuildRamFs(generator);
        }

        var enabledIds = graph
            .Select(rn => rn.Id)
            .ToHashSet(StringComparer.Ordinal);

        var included = manifest.Feature.Where(f => enabledIds.Contains(f.Id));
        var excluded = manifest.Feature.Where(f => !enabledIds.Contains(f.Id));

        Log.Verbose("Generating include targets");

        foreach (ManifestConfig.FeatureConfig node in included)
        {
            generator.AddTarget(string.Format(INCLUDE_ITEM_TEMPLATE, ToPascalCase(node.Id)))
                .Before(TARGET_BEFORE)
                .After(INTERNAL_INCLUDE_SOURCE_TARGET)
                .Message($"Including feature '{node.Id}'")
                .DefineConstants(node.Define);

            Log.Verbose("Added {FeatureId} feature target as INCLUDED", node.Id);
        }

        Log.Verbose("Generating exclude targets");

        foreach (var node in excluded)
        {
            generator.AddTarget(string.Format(EXCLUDE_ITEM_TEMPLATE, ToPascalCase(node.Id)))
                .Before(TARGET_BEFORE)
                .After(INTERNAL_INCLUDE_SOURCE_TARGET)
                .Message($"Excluding feature '{node.Id}'")
                .ExcludeCompile(node.Exclude);

            Log.Verbose("Added {FeatureId} feature target as EXCLUDED", node.Id);
        }

        return generator.Generate();
    }

    private static void BuildRamFs(TargetsFileGenerator generator)
    {
        if (!Directory.Exists(Configuration.DefaultInitRamFsPath) || !Directory.Exists(Configuration.LocalInitRamFsPath))
        {
            Log.Warning("Unable to create initramfs as no directories exists. Run init command.");
            return;
        }

        var target = generator.AddTarget(INTERNAL_RAMFS_TARGET)
                        .Before("PrepareForBuild")
                        .Message("Including initramfs.cpio.gz");

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
            using (FileStream sourceStream = File.OpenRead(Path.Combine(Configuration.OutputDirectoryPath, "initramfs.cpio")))
            {
                using FileStream archiveStream = File.Create(Configuration.GeneratedInitRamFsPath);
                using GZipStream gZip = new(archiveStream, CompressionLevel.Optimal);

                sourceStream.CopyTo(gZip);
            }
        }
        finally
        {
            File.Delete(Path.Combine(Configuration.OutputDirectoryPath, "initramfs.cpio"));
        }

        // Embed file

        target.EmbedResource(Configuration.GeneratedInitRamFsPath, Configuration.InitRamFsLogicalName, $"Exists('{Configuration.GeneratedInitRamFsPath}')");
    }
}
