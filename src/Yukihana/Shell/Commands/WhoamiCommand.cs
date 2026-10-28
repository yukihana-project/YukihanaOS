// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class WhoamiCommand : IShellCommand
{
    public string Name => "whoami";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Print the effective user name";
    public string Usage => "whoami";
    public Capability RequiredCapabilities => Capability.None;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        UserId uid = Kernel.SecurityManager.Current.EffectiveUser;
        context.Out.WriteLine(UserManager.GetUser(uid)?.Name ?? uid.ToString());
        return ShellCommandResult.Ok;
    }
}
