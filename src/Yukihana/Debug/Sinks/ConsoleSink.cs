// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Debug.Interfaces;
using Yukihana.IO;

namespace Yukihana.Debug.Sinks;

internal sealed class ConsoleSink : ILogSink
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    private static readonly Lock s_sinksLock = new();

    public void Write(ReadOnlySpan<char> text)
    {
        lock (s_sinksLock)
        {
            AnsiConsole.WriteLine(text);
        }
    }
}
