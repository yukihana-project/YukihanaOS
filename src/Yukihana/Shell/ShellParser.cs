// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;
using Yukihana.Core.Primitives;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public static class ShellParser
{
    public static Result<IReadOnlyList<ShellCommandLine>, string> Parse(string line)
    {
        List<ShellCommandLine> stages = [];
        List<string> words = [];
        StringBuilder word = new();
        bool inWord = false;
        bool inQuotes = false;

        void EndWord()
        {
            if (inWord)
            {
                words.Add(word.ToString());
                word.Clear();
                inWord = false;
            }
        }

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    inQuotes = false;
                }
                else if (c == '\\' && i + 1 < line.Length && line[i + 1] is '"' or '\\')
                {
                    word.Append(line[++i]);
                }
                else
                {
                    word.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    inWord = true;
                    break;

                case '|':
                    EndWord();

                    if (words.Count == 0)
                    {
                        return Result<IReadOnlyList<ShellCommandLine>, string>.Failure("syntax error near '|'");
                    }

                    stages.Add(new ShellCommandLine(words[0], words.Skip(1).ToArray()));
                    words.Clear();
                    break;

                case ' ':
                case '\t':
                    EndWord();
                    break;

                default:
                    word.Append(c);
                    inWord = true;
                    break;
            }
        }

        if (inQuotes)
        {
            return Result<IReadOnlyList<ShellCommandLine>, string>.Failure("unclosed quote");
        }

        EndWord();

        if (words.Count == 0)
        {
            return stages.Count == 0
                ? Result<IReadOnlyList<ShellCommandLine>, string>.Success([])
                : Result<IReadOnlyList<ShellCommandLine>, string>.Failure("syntax error: expected command after '|'");
        }

        stages.Add(new ShellCommandLine(words[0], words.Skip(1).ToArray()));

        return Result<IReadOnlyList<ShellCommandLine>, string>.Success(stages);
    }
}
