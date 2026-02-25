// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Text;
using ISTAvalon.Models;
using Serilog.Events;

public sealed class ConsoleCaptureScope : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly LineInterceptingTextWriter _stdoutWriter;
    private readonly LineInterceptingTextWriter _stderrWriter;
    private bool _disposed;

    public ConsoleCaptureScope(Action<LogEntry> onEntry)
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;

        _stdoutWriter = new LineInterceptingTextWriter(line =>
            onEntry(new LogEntry(DateTimeOffset.Now, LogEventLevel.Information, $"[stdout] {line}")));
        _stderrWriter = new LineInterceptingTextWriter(line =>
            onEntry(new LogEntry(DateTimeOffset.Now, LogEventLevel.Error, $"[stderr] {line}")));

        Console.SetOut(_stdoutWriter);
        Console.SetError(_stderrWriter);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stdoutWriter.Flush();
        _stderrWriter.Flush();
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    private sealed class LineInterceptingTextWriter(Action<string> onLine) : TextWriter
    {
        private readonly object _gate = new();
        private readonly StringBuilder _buffer = new();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (_gate)
            {
                AppendChar(value);
            }
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            lock (_gate)
            {
                foreach (var ch in value)
                {
                    AppendChar(ch);
                }
            }
        }

        public override void WriteLine(string? value)
        {
            lock (_gate)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _buffer.Append(value);
                }

                EmitLine();
            }
        }

        public override void Flush()
        {
            lock (_gate)
            {
                if (_buffer.Length > 0)
                {
                    EmitLine();
                }
            }
        }

        private void AppendChar(char ch)
        {
            if (ch == '\r')
            {
                return;
            }

            if (ch == '\n')
            {
                EmitLine();
                return;
            }

            _buffer.Append(ch);
        }

        private void EmitLine()
        {
            var line = _buffer.ToString();
            _buffer.Clear();
            onLine(line);
        }
    }
}
