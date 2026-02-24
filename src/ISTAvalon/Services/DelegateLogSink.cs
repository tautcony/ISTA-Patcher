// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using ISTAvalon.Models;
using Serilog.Core;
using Serilog.Events;

public sealed class DelegateLogSink : ILogEventSink
{
    private Action<LogEntry>? _handler;

    public IDisposable Subscribe(Action<LogEntry> handler)
    {
        _handler = handler;
        return new Unsubscriber(() => _handler = null);
    }

    public void Emit(LogEvent logEvent)
    {
        _handler?.Invoke(new LogEntry(logEvent.Timestamp, logEvent.Level, logEvent.RenderMessage()));
    }

    private sealed class Unsubscriber(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
