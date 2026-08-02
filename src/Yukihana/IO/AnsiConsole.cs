// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.IO;

public static class AnsiConsole
{
    private static readonly Dictionary<int, ConsoleColor> ForegroundColors = new()
    {
        [30] = ConsoleColor.Black,
        [31] = ConsoleColor.DarkRed,
        [32] = ConsoleColor.DarkGreen,
        [33] = ConsoleColor.DarkYellow,
        [34] = ConsoleColor.DarkBlue,
        [35] = ConsoleColor.DarkMagenta,
        [36] = ConsoleColor.DarkCyan,
        [37] = ConsoleColor.Gray,

        [90] = ConsoleColor.DarkGray,
        [91] = ConsoleColor.Red,
        [92] = ConsoleColor.Green,
        [93] = ConsoleColor.Yellow,
        [94] = ConsoleColor.Blue,
        [95] = ConsoleColor.Magenta,
        [96] = ConsoleColor.Cyan,
        [97] = ConsoleColor.White,
    };

    private static readonly Dictionary<int, ConsoleColor> BackgroundColors = new()
    {
        [40] = ConsoleColor.Black,
        [41] = ConsoleColor.DarkRed,
        [42] = ConsoleColor.DarkGreen,
        [43] = ConsoleColor.DarkYellow,
        [44] = ConsoleColor.DarkBlue,
        [45] = ConsoleColor.DarkMagenta,
        [46] = ConsoleColor.DarkCyan,
        [47] = ConsoleColor.Gray,

        [100] = ConsoleColor.DarkGray,
        [101] = ConsoleColor.Red,
        [102] = ConsoleColor.Green,
        [103] = ConsoleColor.Yellow,
        [104] = ConsoleColor.Blue,
        [105] = ConsoleColor.Magenta,
        [106] = ConsoleColor.Cyan,
        [107] = ConsoleColor.White,
    };

    public static void Write(string text)
        => Write(text.AsSpan());

    public static void Write(ReadOnlySpan<char> text)
    {
        ConsoleColor defaultForeground = Console.ForegroundColor;
        ConsoleColor defaultBackground = Console.BackgroundColor;

        bool bold = false;
        int segmentStart = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (!IsAnsiSequenceStart(text, index))
            {
                index++;
                continue;
            }

            int codesStart = index + 2;
            int codesEnd = text[codesStart..].IndexOf('m');
            if (codesEnd < 0)
            {
                break;
            }

            codesEnd += codesStart;
            ReadOnlySpan<char> codes = text[codesStart..codesEnd];
            if (!IsAnsiCodes(codes))
            {
                index++;
                continue;
            }

            if (index > segmentStart)
            {
                Console.Write(text[segmentStart..index]);
            }

            if (codes.IsEmpty)
            {
                bold = false;
                Reset(defaultForeground, defaultBackground);
            }
            else
            {
                ApplyCodes(codes, defaultForeground, defaultBackground, ref bold);
            }

            index = codesEnd + 1;
            segmentStart = index;
        }

        if (segmentStart < text.Length)
        {
            Console.Write(text[segmentStart..]);
        }

        Console.ForegroundColor = defaultForeground;
        Console.BackgroundColor = defaultBackground;
    }

    public static void WriteLine(string text)
        => WriteLine(text.AsSpan());

    public static void WriteLine(ReadOnlySpan<char> text)
    {
        Write(text);
        Console.WriteLine();
    }

    private static void ApplyCodes(
        ReadOnlySpan<char> codes,
        ConsoleColor defaultForeground,
        ConsoleColor defaultBackground,
        ref bool bold)
    {
        while (!codes.IsEmpty)
        {
            int separator = codes.IndexOf(';');
            ReadOnlySpan<char> part = separator < 0
                ? codes
                : codes[..separator];

            if (int.TryParse(part, out int code))
            {
                ApplyCode(code, defaultForeground, defaultBackground, ref bold);
            }

            if (separator < 0)
            {
                break;
            }

            codes = codes[(separator + 1)..];
        }
    }

    private static void ApplyCode(
        int code,
        ConsoleColor defaultForeground,
        ConsoleColor defaultBackground,
        ref bool bold)
    {
        switch (code)
        {
            case 0:
                bold = false;
                Reset(defaultForeground, defaultBackground);
                break;

            case 1:
                bold = true;
                break;

            case 22:
                bold = false;
                break;

            case 39:
                Console.ForegroundColor = defaultForeground;
                break;

            case 49:
                Console.BackgroundColor = defaultBackground;
                break;

            default:
                if (ForegroundColors.TryGetValue(code, out var fg))
                {
                    Console.ForegroundColor = bold
                        ? BrightEquivalent(fg)
                        : fg;
                }
                else if (BackgroundColors.TryGetValue(code, out var bg))
                {
                    Console.BackgroundColor = bg;
                }
                break;
        }
    }

    private static bool IsAnsiSequenceStart(ReadOnlySpan<char> text, int index)
        => index + 1 < text.Length
            && text[index] == '\u001b'
            && text[index + 1] == '[';

    private static bool IsAnsiCodes(ReadOnlySpan<char> codes)
    {
        foreach (char code in codes)
        {
            if (code != ';' && !char.IsAsciiDigit(code))
            {
                return false;
            }
        }

        return true;
    }

    private static void Reset(ConsoleColor fg, ConsoleColor bg)
    {
        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;
    }

    private static ConsoleColor BrightEquivalent(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => ConsoleColor.DarkGray,
            ConsoleColor.DarkRed => ConsoleColor.Red,
            ConsoleColor.DarkGreen => ConsoleColor.Green,
            ConsoleColor.DarkYellow => ConsoleColor.Yellow,
            ConsoleColor.DarkBlue => ConsoleColor.Blue,
            ConsoleColor.DarkMagenta => ConsoleColor.Magenta,
            ConsoleColor.DarkCyan => ConsoleColor.Cyan,
            ConsoleColor.Gray => ConsoleColor.White,
            _ => color
        };
    }
}
