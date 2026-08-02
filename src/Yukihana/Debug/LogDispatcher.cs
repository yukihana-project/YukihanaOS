// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Memory;
using Yukihana.Debug.Interfaces;

namespace Yukihana.Debug;

internal static class LogDispatcher
{
    private static readonly List<LogSinkRegistration> s_sinks = [];
    private static readonly RingBuffer<LogEvent> s_ringBuffer = new(1024);

    public static bool RingBufferEnabled { get; set; } = true;
    public static int RingBufferCapacity => s_ringBuffer.Capacity;
    public static int RingBufferCount => s_ringBuffer.Count;

    public static IReadOnlyList<LogSinkRegistration> Sinks => s_sinks;

    private static readonly Lock s_ringBufferLock = new();

    public static void RegisterSink(
        ILogSink sink,
        ILogFormatter formatter,
        LogLevel minimumLevel = LogLevel.Trace,
        bool enabled = true)
    {
        s_sinks.Add(new LogSinkRegistration
        {
            Sink = sink,
            Formatter = formatter,
            MinimumLevel = minimumLevel,
            Enabled = enabled
        });
    }

    public static bool RemoveSink(ILogSink sink)
    {
        for (int i = 0; i < s_sinks.Count; i++)
        {
            if (ReferenceEquals(s_sinks[i].Sink, sink))
            {
                s_sinks.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public static void Dispatch(in LogEvent logEvent)
    {
        lock (s_ringBufferLock)
        {
            if (RingBufferEnabled)
            {
                s_ringBuffer.Add(logEvent);
            }
        }

        foreach (LogSinkRegistration registration in s_sinks.Where(registration => registration.Enabled))
        {
            if (logEvent.Level < registration.MinimumLevel)
            {
                continue;
            }
            string text = registration.Formatter.Format(logEvent);

            registration.Sink.Write(text);
        }
    }

    public static void Snapshot(Span<LogEvent> logsOut)
    {
        s_ringBuffer.CopyTo(logsOut);
    }
}
