// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Spectre.Console;

namespace Yukihana.BuildConfig.Menu;

internal static class MainPage
{
    public static void Show()
    {
        while (true)
        {
            Process();
        }
    }

    private static void Process()
    {
        AnsiConsole.Clear();

        DrawHeader();

        string[] options = [
            "Configure Features",
            //"Presets",
            //"Validate",
            //"Generate",
            //"Clean",
            "Exit"
        ];

        string option = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .PageSize(5)
            .WrapAround()
            .AddChoices(options));

        switch (option)
        {
            case "Configure Features":
                ConfigPage.Show();
                break;
            case "Presets":
            case "Validate":
            case "Generate":
            case "Clean":
            case "Exit":
                AnsiConsole.Clear();
                Environment.Exit(0);
                break;
        }
    }

    private static void DrawHeader()
    {
        AnsiConsole.MarkupLine("[aqua]Yukihana YKConfig[/]\n\n");
        AnsiConsole.MarkupLine("[gray]Select and option:[/]");
    }
}
