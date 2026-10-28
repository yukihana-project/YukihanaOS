// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Commands;
using Yukihana.Shell.Exceptions;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public sealed class ShellCommandDispatcher(ShellCommandRegistry registry)
{
    private static ShellCommandResult Invoke(IShellCommand command, ShellCommandContext context)
    {
        try
        {
            return command.Execute(context);
        }
        catch (ShellCommandCancelledException)
        {
            return ShellCommandResult.CancelledInstance;
        }
        catch (Exception ex)
        {
            return new ShellCommandResult.Error(ex.Message);
        }
    }

    public ShellCommandResult Dispatch(ShellSession session, IReadOnlyList<ShellCommandLine> stages)
    {
        ShellCommandResult result = ShellCommandResult.Ok;
        TextReader input = Console.In;

        for (int i = 0; i < stages.Count; i++)
        {
            bool isLast = i == stages.Count - 1;
            StringWriter buffer = new();

            result = DispatchCommand(session, stages[i], input, isLast ? Console.Out : buffer);

            if (result is not ShellCommandResult.Success)
            {
                return result;
            }

            input = new StringReader(buffer.ToString());
        }

        return result;
    }

    // Public so that sudo can run one command with its own streams
    public ShellCommandResult DispatchCommand(
        ShellSession session,
        ShellCommandLine line,
        TextReader input,
        TextWriter output)
    {
        IShellCommand? command = registry.Find(line.Command);

        if (command is null)
        {
            return new ShellCommandResult.NotFound(line.Command);
        }

        Capability required = command.RequiredCapabilities;

        if (required != Capability.None &&
            !Kernel.SecurityManager.Current.HasAllCapabilities(required))
        {
            return new ShellCommandResult.PermissionDenied(
                $"{line.Command}: permission denied");
        }

        ShellCommandContext context = new()
        {
            Session = session,
            Arguments = line.Arguments,
            In = input,
            Out = output,
            Err = Console.Error,
            Cancellation = new ShellCancellation()
        };

        // Runs on the shell thread: console I/O is only routed to a thread bound to a console session
        return Invoke(command, context);
    }
}
