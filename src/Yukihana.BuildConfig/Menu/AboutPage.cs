// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Spectre.Console;

namespace Yukihana.BuildConfig.Menu;

internal static class AboutPage
{
    public static void Show()
    {
        AnsiConsole.Clear();

        DrawHeader();

        AnsiConsole.MarkupLine($"YKConfig v{typeof(Program).Assembly.GetName().Version}");
        AnsiConsole.MarkupLine($"[gray]Made by Yukihana OS Contributors[/]");

        string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .PageSize(3)
            .AddChoices(["Back", "Exit"]));

        switch (selected)
        {
            case "Back":
                return;
            case "Exit":
                AnsiConsole.Clear();
                Environment.Exit(1);
                return;
        }
    }

    private static void DrawHeader()
    {
        AnsiConsole.MarkupLine("[aqua]Yukihana YKConfig[/]\n\n");
    }
}
