// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Shell;

// Colors only reach the screen. Piped output is plain, since the console color is global state and would leak into the next stage
public static class TerminalStyle
{
    public static void Write(TextWriter output, bool terminal, ConsoleColor? color, string text)
    {
        if (!terminal || color is null)
        {
            output.Write(text);
            return;
        }

        Console.ForegroundColor = color.Value;
        output.Write(text);
        Console.ResetColor();
    }

    public static void WriteLine(TextWriter output, bool terminal, ConsoleColor? color, string text)
    {
        Write(output, terminal, color, text + "\n");
    }
}
