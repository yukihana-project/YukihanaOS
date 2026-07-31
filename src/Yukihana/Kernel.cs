// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Data;
using System.Reflection;
using Cosmos.Kernel.HAL.Vfs;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Cosmos.Kernel.System.Storage;
using Cosmos.Kernel.System.Vfs;
using Yukihana.Boot;
using Yukihana.Core.Compression;
using Yukihana.Core.Compression.Archives;
using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.Extensions.System;
using Yukihana.Core.Primitives;
using Yukihana.Debug;
using Yukihana.Debug.Formatters;
using Yukihana.Debug.Sinks;
using Yukihana.IO.Loaders;
using Yukihana.IO.Loaders.Optional;
using Yukihana.Resources;
using Yukihana.Security;
using Yukihana.Vfs;
using Yukihana.Vfs.Config;
using Yukihana.Vfs.Device;
using Yukihana.Vfs.Filesystem.InitFs;
using Sys = Cosmos.Kernel.System;

namespace Yukihana;

public sealed class Kernel : Sys.Kernel
{
    public static DateTime BootTime { get; }

    private const string RamfsPath = "Yukihana";
    private const string RamfsFile = "initramfs.cpio.gz";

    private static string RamfsResourceKey => string.Join('.', RamfsPath, RamfsFile);

    private static readonly Logger s_kernelLogger;
    private static readonly VfsConfigManager s_vfsMan;

    public static readonly SecurityManager SecurityManager = new();

    static Kernel()
    {
        BootTime = DateTime.Now;

        s_kernelLogger = new Logger("kern");

        s_vfsMan = new VfsConfigManager();
    }

    protected override void BeforeRun()
    {
        try
        {
            s_kernelLogger.Info($"Booted at {BootTime:dd-MM-yyyy HH:mm:ss.fff}");

            string argsStr = Environment.CommandLine;
            BootArguments args = BootArgumentParser.Parse(argsStr);

            Init(args);
        }
        catch (Exception ex)
        {
            s_kernelLogger.Critical("Caught exception during Init()");
            ex.Panic("Unhandles exception during boot");
        }
    }

    private void Init(BootArguments args)
    {
        //
        // STAGE 1 -> Early boot
        //

        LogDispatcher.RegisterSink(new ConsoleSink(), new DeltaAnsiFormatter());
#if FEATURE_SERIAL_SINK
        LogDispatcher.RegisterSink(new SerialSink(), new DeltaFormatter());
#endif

        Logger.GlobalLogger = s_kernelLogger;

        // Setup formatters and sinks
        var logger = new Logger("init");

        SecurityManager.Set(Thread.CurrentThread, SecurityContextFactory.CreateKernelContext());

        logger.Trace("Parsed arguments:");

        foreach (BootArgument arg in args.AsSpan())
        {
            logger.Trace($"  -> {arg}");
        }

        Option<int> logLevel = args.GetInt32("loglevel");
        LogLevel consoleLogLevel = logLevel.Map(
            value => (LogLevel)value,
            () => LogLevel.Info);

        foreach (LogSinkRegistration sinkReg in LogDispatcher.Sinks)
        {
            if (sinkReg.Sink is ConsoleSink)
            {
                sinkReg.MinimumLevel = consoleLogLevel;
            }
        }

        logger.Debug("Updated console sink log level");

        VfsInit.InitVfs(logger, s_vfsMan);

        logger.Info($"Fetching \"{RamfsFile}\".");
        byte[]? ramfsBytes = null;

        var assembly = Assembly.GetExecutingAssembly();

        using (var result = assembly.GetManifestResourceStream(RamfsResourceKey).ToOption())
        using (var memStream = new MemoryStream())
        {
            if (result.IsSome)
            {
                Stream stream = result.Value;
                stream.CopyTo(memStream);
                ramfsBytes = memStream.ToArray();
            }
            else
            {
                logger.Warn("No initramfs archive provided.");
            }
        }

        logger.Info($"Discovered {StorageManager.Partitions.Count} partitions");

        ArchiveImage? ramfsImage = null;
        if (ramfsBytes is not null)
        {
            ramfsImage = LoadInitRamFs(ramfsBytes, logger); // This throws if cannot read
        }

        if (ramfsImage is not null)
        {
            MemoryBlockDevice ramfsDisk = new("INITFSDISK", 512, 65536);
            InitfsFilesystemType initfsType = new(ramfsDisk, ramfsImage);

            if (VfsManager.RegisterFilesystem("initfs", initfsType))
            {
                logger.Info("Registered initfs");
            }
            else
            {
                KernelPanic.Panic("Failed to register initfs");
            }

            logger.Info("Trying to mount initfs as root");

            if (VfsManager.TryMount("initfs", "", MountFlags.ReadOnly, "/", out _))
            {
                logger.Info("Mounted initfs as '/'");
            }
            else
            {
                KernelPanic.Panic("Failed to mount initfs as '/'");
            }
        }

        var fontGroup = new OptionalResourceGroup<FontState>(
            name: "Fonts",
            createState: () => new FontState(),
            commit: _ => { },
            provider: new VfsResourceProvider()
        );

        fontGroup.Add(
            relativePath: "/usr/share/fonts/zap-ext-light18.psf",
            description: "Console font",
            applyCallback: (s, data) => s.Font = PCScreenFont.LoadFont(data)
        );

        fontGroup.TryLoad().Switch(
            some =>
            {
                KernelConsole.Default!.Font = some.Font;
                logger.Info("Applyied font to console.");
            },
            none: () => { }
        );

        logger.Info("Reading fstab");
        if (File.Exists("/etc/fstab"))
        {
            string content = File.ReadAllText("/etc/fstab");
            s_vfsMan.LoadConfig(content);

            logger.Info("Unmounting initfs");
            if (!VfsManager.TryUnmount("/"))
            {
                logger.Error("Failed to unmount initfs");
            }
            else
            {
                logger.Info("Unmounted initfs");
            }

            s_vfsMan.TryMountAll(out _);
        }
        else
        {
            logger.Error("Unable to locate fstab! No filesystem will be mounted");
        }

        /*
        logger.Warn("Mounting /tmp as FAT 16, bypassing fstab");

        // Mount ram backed FAT as tmp
        MemoryBlockDevice ramDisk = new("RAMDISK", 512, 65536);
        FatFilesystemType ramFat = new(ramDisk);

        VfsInit.s_filesystemTypes["fat"].TryFormat(default, new FatFormatOptions { Type = FatType.Fat16 });

        VfsManager.RegisterFilesystem("ramfat", ramFat);
        VfsManager.TryMount("ramfat", "", MountFlags.None, "/tmp", out _);
        */

        logger.Info($"Base kernel initialization finished at {DateTime.Now:dd-MM-yyyy HH:mm:ss.fff}.");

        //logger.Info("Testing new thread context");
        //
        //Thread thread = SecurityManager.CreateThread(() =>
        //{
        //    s_kernelLogger.Info($"Current context is: {SecurityManager.Current}");
        //});
        //
        //
        //s_kernelLogger.Info("This should inherit from kernel");
        //thread.Start();
        //thread.Join();
        //
        //thread = SecurityManager.CreateThread(() =>
        //{
        //    s_kernelLogger.Info($"Current context is: {SecurityManager.Current}");
        //}, SecurityContextFactory.CreateRootContext());
        //
        //s_kernelLogger.Info("This should be just root context");
        //thread.Start();
        //thread.Join();

        throw new Exception("Returned from init");
    }

    protected override void Run()
    { }

    protected override void AfterRun()
    {
        Console.WriteLine("\n");

        s_kernelLogger.Info("Reached AfterRun().");

        s_kernelLogger.Info("Kernel cleanup finished.");
        s_kernelLogger.Info("Bye.");
    }

    private ArchiveImage LoadInitRamFs(byte[] data, Logger logger)
    {
        logger.Info("Has initramfs provided");

        // First, check if initramfs is no compressed
        Option<IArchivator> archivator = ArchivatorFactory.Detect(data);

        if (archivator.IsSome)
        {
            return archivator.Value.Read(data);
        }

        // We couldn't detect, so check if it's compressed
        IArchiveCompressor compressor = ArchiveCompressorFactory.Detect(data)
            .OrThrow("initramfs image is unsupported (either archive image or compression)");

        byte[] decompressed = compressor.Decompress(data);

        archivator = ArchivatorFactory.Detect(decompressed);

        if (archivator.IsNone)
        {
            throw new DataException("initramfs image is not supported");
        }

        return archivator.Value.Read(decompressed);
    }
}
