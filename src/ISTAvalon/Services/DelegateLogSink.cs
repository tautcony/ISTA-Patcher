// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using Serilog.Core;
using Serilog.Events;

public sealed class DelegateLogSink : ILogEventSink
{
    private Action<string>? _handler;

    public IDisposable Subscribe(Action<string> handler)
    {
        _handler = handler;
        return new Unsubscriber(() => _handler = null);
    }

    public void Emit(LogEvent logEvent)
    {
        _handler?.Invoke(logEvent.RenderMessage());
    }

    private sealed class Unsubscriber(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
