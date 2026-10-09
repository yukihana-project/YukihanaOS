// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem.Fat;
using Cosmos.Kernel.System.FileSystem;
using Yukihana.Debug;
using Yukihana.Vfs.Config;

namespace Yukihana.Vfs;

internal static class VfsInit
{
    internal static readonly Dictionary<string, IVfsFileSystemType> s_filesystemTypes = new(StringComparer.Ordinal)
    {
        { "fat", new FatFileSystemType() },
    };

    public static void InitVfs(Logger logger, VfsConfigManager vfsMan)
    {
        foreach ((string name, IVfsFileSystemType type) in s_filesystemTypes)
        {
            logger.Info($"Registering fs type '{name}'");
            vfsMan.RegisterFilesystem(name, type);
            if (!VfsManager.RegisterFileSystem(name, type))
            {
                logger.Error("Unable to register filesystem!");
            }
            else
            {
                logger.Info("Registered filesystem successfully");
            }
        }
    }
}
