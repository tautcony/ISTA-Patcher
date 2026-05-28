// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAvalon.Models;
using ISTAvalon.Services;
using Serilog.Events;

public class ConsoleCaptureTests
{
    [Test]
    public void ConsoleCaptureScope_CapturesStdoutAndStderr_WithLevelMapping()
    {
        var entries = new List<LogEntry>();

        using (new ConsoleCaptureScope(entries.Add))
        {
            Console.WriteLine("hello");
            Console.Error.WriteLine("oops");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Any(e => e.Level == LogEventLevel.Information && e.Message == "[stdout] hello"), Is.True);
            Assert.That(entries.Any(e => e.Level == LogEventLevel.Error && e.Message == "[stderr] oops"), Is.True);
        }
    }

    [Test]
    public void ConsoleCaptureScope_RestoresOriginalWriters_AfterDispose()
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using (new ConsoleCaptureScope(_ => { }))
        {
            Console.WriteLine("inside scope");
            Console.Error.WriteLine("inside scope err");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Console.Out, Is.SameAs(originalOut));
            Assert.That(Console.Error, Is.SameAs(originalErr));
        }
    }

    [Test]
    public async Task CommandExecutionService_ForwardConsoleOutputToLogSink()
    {
        var entries = new List<LogEntry>();
        using var subscription = ISTAvalon.App.LogSink.Subscribe(entries.Add);

        var descriptor = new CommandDescriptor
        {
            Name = "console-write-command",
            CommandType = typeof(ConsoleWriteCommand),
            Parameters = [],
            Subcommands = [],
        };

        var result = await CommandExecutionService.ExecuteAsync(descriptor, []);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Zero);
            Assert.That(entries.Any(e => e.Message.Contains("[stdout] line from stdout", StringComparison.Ordinal)), Is.True);
            Assert.That(entries.Any(e => e.Message.Contains("[stderr] line from stderr", StringComparison.Ordinal)), Is.True);
        }
    }

    [Test]
    public async Task CommandExecutionService_DoesNotLeakConsoleRedirection_BetweenSequentialRuns()
    {
        var entries = new List<LogEntry>();
        using var subscription = ISTAvalon.App.LogSink.Subscribe(entries.Add);

        var descriptor = new CommandDescriptor
        {
            Name = "console-write-command",
            CommandType = typeof(ConsoleWriteCommand),
            Parameters = [],
            Subcommands = [],
        };

        await CommandExecutionService.ExecuteAsync(descriptor, []);
        await CommandExecutionService.ExecuteAsync(descriptor, []);

        var stdoutLines = entries.Count(e => e.Message.Contains("[stdout] line from stdout", StringComparison.Ordinal));
        var stderrLines = entries.Count(e => e.Message.Contains("[stderr] line from stderr", StringComparison.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stdoutLines, Is.EqualTo(2));
            Assert.That(stderrLines, Is.EqualTo(2));
        }
    }

    private sealed class ConsoleWriteCommand
    {
        public Task<int> RunAsync()
        {
            Console.WriteLine("line from stdout");
            Console.Error.WriteLine("line from stderr");
            return Task.FromResult(0);
        }
    }

    // ────────────── LineInterceptingTextWriter low-level paths ──────────────

    [Test]
    public void ConsoleCaptureScope_WriteChar_EmitsLineOnNewline()
    {
        var entries = new List<LogEntry>();
        using var scope = new ConsoleCaptureScope(entries.Add);

        // Write individual characters including a newline
        Console.Out.Write('h');
        Console.Out.Write('i');
        Console.Out.Write('\n');

        Assert.That(entries.Any(e => e.Message.Contains("hi", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ConsoleCaptureScope_WriteChar_IgnoresCarriageReturn()
    {
        var entries = new List<LogEntry>();
        using var scope = new ConsoleCaptureScope(entries.Add);

        // \r should be stripped; \n triggers emission
        Console.Out.Write('o');
        Console.Out.Write('k');
        Console.Out.Write('\r');
        Console.Out.Write('\n');

        Assert.That(entries.Any(e => e.Message.Contains("ok", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ConsoleCaptureScope_WriteString_WithEmbeddedNewlines_EmitsMultipleLines()
    {
        var entries = new List<LogEntry>();
        using var scope = new ConsoleCaptureScope(entries.Add);

        Console.Out.Write("line1\nline2\n");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Any(e => e.Message.Contains("line1", StringComparison.Ordinal)), Is.True);
            Assert.That(entries.Any(e => e.Message.Contains("line2", StringComparison.Ordinal)), Is.True);
        }
    }

    [Test]
    public void ConsoleCaptureScope_WriteString_NullOrEmpty_DoesNotEmit()
    {
        var entries = new List<LogEntry>();
        using var scope = new ConsoleCaptureScope(entries.Add);

        Console.Out.Write((string?)null);
        Console.Out.Write(string.Empty);

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public void ConsoleCaptureScope_Flush_WithPartialBuffer_EmitsLine()
    {
        var entries = new List<LogEntry>();

        // Create scope and write without a trailing newline; Dispose should flush
        using (new ConsoleCaptureScope(entries.Add))
        {
            Console.Out.Write("partial");
            // No newline — the Dispose/Flush should still emit this
        }

        Assert.That(entries.Any(e => e.Message.Contains("partial", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ConsoleCaptureScope_DoubleDispose_IsIdempotent()
    {
        var entries = new List<LogEntry>();
        var scope = new ConsoleCaptureScope(entries.Add);

        Assert.DoesNotThrow(() =>
        {
            scope.Dispose();
            scope.Dispose(); // second dispose should not throw
        });
    }
}
