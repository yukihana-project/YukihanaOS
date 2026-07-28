// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;
using Yukihana.Debug;

namespace Yukihana.Security;

public sealed class SecurityManager
{
    private readonly ConcurrentDictionary<Thread, SecurityContext> _contexts = [];

    public SecurityContext? Get(Thread thread)
    {
        _contexts.TryGetValue(thread, out SecurityContext? context);
        return context;
    }

    public void Set(Thread thread, SecurityContext context)
    {
        Logger.GlobalLogger.Trace($"Updating thread #{thread.ManagedThreadId} credentials");
        _contexts[thread] = context;
    }

    public void Remove(Thread thread)
    {
        Logger.GlobalLogger.Trace($"Removing thread #{thread.ManagedThreadId} from contexts");
        _contexts.TryRemove(thread, out _);
    }

    public SecurityContext Current
        => Get(Thread.CurrentThread)
           ?? throw new InvalidOperationException();
}
