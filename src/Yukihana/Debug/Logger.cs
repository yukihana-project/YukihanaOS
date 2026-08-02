// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime.CompilerServices;

namespace Yukihana.Debug;

public sealed class Logger(string source)
{
    public static Logger GlobalLogger
    {
        get
        {
            return field ?? new Logger();
        }
        set;
    } = null;

    public Logger() : this(string.Empty)
    { }

    public void Trace(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) => LogDispatcher.Dispatch(new LogEvent(LogLevel.Trace, source, msg, DateTime.Now, 0, f, m, l));
    public void Debug(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) => LogDispatcher.Dispatch(new LogEvent(LogLevel.Debug, source, msg, DateTime.Now, 0, f, m, l));

    public void Info(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) => LogDispatcher.Dispatch(new LogEvent(LogLevel.Info, source, msg, DateTime.Now, 0, f, m, l));

    public void Warn(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) => LogDispatcher.Dispatch(new LogEvent(LogLevel.Warn, source, msg, DateTime.Now, 0, f, m, l));

    public void Error(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) => LogDispatcher.Dispatch(new LogEvent(LogLevel.Error, source, msg, DateTime.Now, 0, f, m, l));

    public void Critical(string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "",
        [CallerLineNumber] int l = 0) => LogDispatcher.Dispatch(new LogEvent(LogLevel.Crit, source, msg, DateTime.Now, 0, f, m, l));
}
