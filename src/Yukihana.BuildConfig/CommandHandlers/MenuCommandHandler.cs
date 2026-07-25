// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using Yukihana.BuildConfig.Menu;

namespace Yukihana.BuildConfig.CommandHandlers;

internal sealed class MenuCommandHandler
{
    public static int Handle(ParseResult result)
    {
        MainPage.Show();
        return 0;
    }
}