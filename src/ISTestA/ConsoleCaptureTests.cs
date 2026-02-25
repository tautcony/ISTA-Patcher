// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAvalon.Models;
using ISTAvalon.Services;
using ISTAvalon.ViewModels;
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

        Assert.That(entries.Any(e => e.Level == LogEventLevel.Information && e.Message == "[stdout] hello"), Is.True);
        Assert.That(entries.Any(e => e.Level == LogEventLevel.Error && e.Message == "[stderr] oops"), Is.True);
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

        Assert.That(Console.Out, Is.SameAs(originalOut));
        Assert.That(Console.Error, Is.SameAs(originalErr));
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

        var result = await CommandExecutionService.ExecuteAsync(descriptor, Array.Empty<ParameterViewModel>());

        Assert.That(result, Is.EqualTo(0));
        Assert.That(entries.Any(e => e.Message.Contains("[stdout] line from stdout", StringComparison.Ordinal)), Is.True);
        Assert.That(entries.Any(e => e.Message.Contains("[stderr] line from stderr", StringComparison.Ordinal)), Is.True);
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

        await CommandExecutionService.ExecuteAsync(descriptor, Array.Empty<ParameterViewModel>());
        await CommandExecutionService.ExecuteAsync(descriptor, Array.Empty<ParameterViewModel>());

        var stdoutLines = entries.Count(e => e.Message.Contains("[stdout] line from stdout", StringComparison.Ordinal));
        var stderrLines = entries.Count(e => e.Message.Contains("[stderr] line from stderr", StringComparison.Ordinal));

        Assert.That(stdoutLines, Is.EqualTo(2));
        Assert.That(stderrLines, Is.EqualTo(2));
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
}
