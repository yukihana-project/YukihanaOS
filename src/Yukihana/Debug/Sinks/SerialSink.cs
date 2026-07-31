// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.Core.IO;
using Yukihana.Debug.Interfaces;

namespace Yukihana.Debug.Sinks;

internal sealed class SerialSink : ILogSink
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
    private static readonly Lock s_sinksLock = new();

    public void Write(ReadOnlySpan<char> text)
    {
        lock(s_sinksLock)
        {
            Serial.WriteString(new string(text) + '\n');
        }
    }
}
