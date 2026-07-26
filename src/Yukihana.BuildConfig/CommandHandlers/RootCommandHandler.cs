// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Spectre.Console;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class RootCommandHandler
{
    public static int Handle(ParseResult result)
    {
        bool isVerbose = result.GetValue(Globals.Args.VerboseOption);
        bool isQuiet = result.GetValue(Globals.Args.QuietOption);
        bool noColor = result.GetValue(Globals.Args.NoColorOption);

        if (isVerbose)
        {
            Globals.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
        }
        else if (isQuiet)
        {
            Globals.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Error;
        }

        if (noColor)
        {
            AnsiConsole.Profile.Capabilities.Ansi = false;
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        }

        return 0;
    }
}
