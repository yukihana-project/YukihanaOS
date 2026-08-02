// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public static class PermissionManager
{
    public static bool CanMount() => Kernel.SecurityManager.Current.HasCapability(Capability.MountFilesystem);
    public static bool CanShutdown() => Kernel.SecurityManager.Current.HasCapability(Capability.Shutdown);
    public static bool CanChmod() => Kernel.SecurityManager.Current.HasCapability(Capability.ChangeMode);
    public static bool CanChpermissions() => Kernel.SecurityManager.Current.HasCapability(Capability.ChangePermissions);
}
