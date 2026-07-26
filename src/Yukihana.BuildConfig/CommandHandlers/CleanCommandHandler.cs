// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;

namespace Yukihana.BuildConfig.CommandHandlers;

internal static class CleanCommandHandler
{
    public static int Handle(ParseResult result)
    {
        bool doCleanCurrent = result.GetValue(Globals.Args.AllOption);

        string[] files = [.. Directory.EnumerateFiles(Globals.OutputDirectoryPath)];

        foreach (string path in files)
        {
            File.Delete(path);
        }

        File.Delete(Globals.GetManifestClosePath("State.toml"));

        if (doCleanCurrent)
        {
            File.Delete(Globals.GetManifestClosePath("Current.toml"));
        }

        return 0;
    }
}
