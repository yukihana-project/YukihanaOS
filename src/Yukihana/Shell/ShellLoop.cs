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
            char promptChar = session.User.Id == UserId.Root ? '#' : '$';
            Console.Write($"{session.User.Name}@yukihana:{session.CurrentDirectory}{promptChar} ");

            string? line = Console.ReadLine();

            if (line is null)
            {
                return;
            }

            Result<IReadOnlyList<ShellCommandLine>, string> parsed = ShellParser.Parse(line);

            if (parsed.IsFailure)
            {
                Console.Error.WriteLine(parsed.Error);
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
                Console.Error.WriteLine(message);
            }
        }
    }
}
