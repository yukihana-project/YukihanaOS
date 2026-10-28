// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Primitives;
using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public static class ShellLoop
{
    public static void Run(ShellSession session, ShellCommandDispatcher dispatcher)
    {
        while (!session.ExitRequested)
        {
            bool isRoot = session.User.Id == UserId.Root;

            TerminalStyle.Write(Console.Out, true, isRoot ? ConsoleColor.Red : ConsoleColor.Green, session.User.Name);
            Console.Write("@yukihana:");
            TerminalStyle.Write(Console.Out, true, ConsoleColor.Cyan, session.CurrentDirectory);
            Console.Write(isRoot ? "# " : "$ ");

            string? line = Console.ReadLine();

            if (line is null)
            {
                return;
            }

            Result<IReadOnlyList<ShellCommandLine>, string> parsed = ShellParser.Parse(line);

            if (parsed.IsFailure)
            {
                TerminalStyle.WriteLine(Console.Error, true, ConsoleColor.Red, parsed.Error);
                continue;
            }

            if (parsed.Value.Count == 0)
            {
                continue;
            }

            ShellCommandResult result = dispatcher.Dispatch(session, parsed.Value);

            string? message = result switch
            {
                ShellCommandResult.NotFound notFound => notFound.Message,
                ShellCommandResult.PermissionDenied denied => denied.Message,
                ShellCommandResult.Error error => error.Message,
                _ => null
            };

            if (message is not null)
            {
                TerminalStyle.WriteLine(Console.Error, true, ConsoleColor.Red, message);
            }
        }
    }
}
