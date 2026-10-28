// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell.Commands;

public static class BuiltinCommands
{
    public static ShellCommandRegistry CreateRegistry()
    {
        ShellCommandRegistry registry = new();

        registry.Register(new HelpCommand(registry));
        registry.Register(new EchoCommand());
        registry.Register(new ClearCommand());
        registry.Register(new ExitCommand());
        registry.Register(new WhoamiCommand());

        registry.Register(new PwdCommand());
        registry.Register(new CdCommand());
        registry.Register(new LsCommand());
        registry.Register(new CatCommand());

        registry.Register(new MountCommand());
        registry.Register(new UmountCommand());
        registry.Register(new ShutdownCommand());
        registry.Register(new ChmodCommand());

        registry.Register(new UseraddCommand());
        registry.Register(new PasswdCommand());
        registry.Register(new UserdelCommand());
        registry.Register(new GroupaddCommand());
        registry.Register(new UsermodCommand());

        registry.Register(new MkdirCommand());
        registry.Register(new RmdirCommand());
        registry.Register(new RmCommand());
        registry.Register(new TouchCommand());
        registry.Register(new MvCommand());
        registry.Register(new CpCommand());
        registry.Register(new StatCommand());

        registry.Register(new SudoCommand(registry));

        return registry;
    }
}
