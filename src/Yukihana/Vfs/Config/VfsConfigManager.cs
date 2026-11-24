// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.System.FileSystem;
using Cosmos.Kernel.System.Storage;
using Yukihana.Debug;
using Yukihana.Vfs.Probe;

namespace Yukihana.Vfs.Config;

// fstab handling lives in VfsConfigManager.Fstab.cs, which the fstab feature owns
public sealed partial class VfsConfigManager
{
    private readonly Dictionary<string, IVfsFileSystemType> _filesystemTypes = [];

    private readonly Logger _logger = new("vfsman");

    public void RegisterFilesystem(string name, IVfsFileSystemType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);

        _logger.Info($"Registered filesystem with name '{name}' of type '{type.GetType().Name}'");

        _filesystemTypes[name] = type;
    }

    /// <summary>
    /// Mounts the first partition that probes as a known filesystem. Used when there is no initramfs root.
    /// </summary>
    public bool TryMountFirstVolume(string mountPoint, MountFlags flags, out VfsMount? mount)
    {
        mount = null;
        IReadOnlyList<Partition> partitions = StorageManager.Partitions;

        for (int i = 0; i < partitions.Count; i++)
        {
            if (!FilesystemProber.ProbeFilesystem(partitions[i], out FilesystemProbeResult result))
            {
                continue;
            }

            _logger.Info($"Found '{result.Filesystem}' volume at index '{i}'");

            // shortcut: only FAT is probed and registered, so probe names (fat12/16/32) map to "fat". Upgrade when more types arrive
            return VfsManager.TryMount("fat", $"{i}", flags, mountPoint, out mount);
        }

        _logger.Error("No known volume found");
        return false;
    }

    public VfsMount? Mount(string filesystemType, string source, MountFlags flags, string mountPoint)
    {
        if (VfsManager.TryMount(filesystemType, source, flags, mountPoint, out VfsMount? mount))
        {
            return mount;
        }

        return null;
    }

    public bool Unmount(string mountPoint)
    {
        return VfsManager.TryUnmount(mountPoint);
    }
}
