// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell.Commands;

public sealed class UseraddCommand : IShellCommand
{
    public string Name => "useradd";
    public IReadOnlyList<string> Aliases => [];
    public string Summary => "Create a user account";
    public string Usage => "useradd <name>";
    public Capability RequiredCapabilities => Capability.ManageUsers;
    public ShellCommandFlags Flags => ShellCommandFlags.None;

    public ShellCommandResult Execute(ShellCommandContext context)
    {
        if (context.Arguments.Count != 1)
        {
            return new ShellCommandResult.Error($"usage: {Usage}");
        }

        string name = context.Arguments[0];

        if (!UserDatabase.IsValidName(name))
        {
            return new ShellCommandResult.Error($"useradd: invalid user name '{name}'");
        }

        if (UserManager.GetUser(name) is not null)
        {
            return new ShellCommandResult.Error($"useradd: user '{name}' already exists");
        }

        // shortcut: every new user gets primary group 1000 and no capabilities, there is no group command yet
        UserManager.AddUser(name, CapabilitySet.Default, new GroupId(1000));
        User user = UserManager.GetUser(name)!;

        AccountManager.SetPassword(user, PasswordPrompt.ReadNew());

        return UserDatabase.Save()
            ? ShellCommandResult.Ok
            : new ShellCommandResult.Error("useradd: created in memory, but could not save /etc/passwd");
    }
}
