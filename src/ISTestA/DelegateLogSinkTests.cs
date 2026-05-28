// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAvalon.Models;
using ISTAvalon.Services;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

public class DelegateLogSinkTests
{
    [Test]
    public void Subscribe_ReceivesPublishedEntries()
    {
        var sink = new DelegateLogSink();
        var received = new List<LogEntry>();
        using var _ = sink.Subscribe(received.Add);

        sink.Publish(new LogEntry(DateTimeOffset.Now, LogEventLevel.Information, "hello"));

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Message, Is.EqualTo("hello"));
    }

    [Test]
    public void Unsubscribe_StopsReceivingEntries()
    {
        var sink = new DelegateLogSink();
        var received = new List<LogEntry>();
        var sub = sink.Subscribe(received.Add);

        sub.Dispose();
        sink.Publish(new LogEntry(DateTimeOffset.Now, LogEventLevel.Information, "after"));

        Assert.That(received, Is.Empty);
    }

    [Test]
    public void Publish_WithNoSubscriber_DoesNotThrow()
    {
        var sink = new DelegateLogSink();

        Assert.DoesNotThrow(() =>
            sink.Publish(new LogEntry(DateTimeOffset.Now, LogEventLevel.Warning, "no-sub")));
    }

    [Test]
    public void Emit_WithSubscriber_ForwardsRenderedMessage()
    {
        var sink = new DelegateLogSink();
        var received = new List<LogEntry>();
        using var _ = sink.Subscribe(received.Add);

        var template = new MessageTemplateParser().Parse("Test {Value}");
        var props = new List<LogEventProperty>
        {
            new("Value", new ScalarValue(42)),
        };
        var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Warning, null, template, props);

        sink.Emit(logEvent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].Level, Is.EqualTo(LogEventLevel.Warning));
            Assert.That(received[0].Message, Does.Contain("42"));
        }
    }

    [Test]
    public void Emit_WithNoSubscriber_DoesNotThrow()
    {
        var sink = new DelegateLogSink();
        var template = new MessageTemplateParser().Parse("msg");
        var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, template, []);

        Assert.DoesNotThrow(() => sink.Emit(logEvent));
    }
}
