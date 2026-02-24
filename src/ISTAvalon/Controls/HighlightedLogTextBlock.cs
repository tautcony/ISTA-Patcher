// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using ISTAvalon.Converters;
using ISTAvalon.Models;

/// <summary>
/// Custom <see cref="SelectableTextBlock"/> that accepts a <see cref="LogEntry"/>
/// and populates its <see cref="InlineCollection"/> with syntax-highlighted runs.
/// </summary>
public class HighlightedLogTextBlock : SelectableTextBlock
{
    public static readonly StyledProperty<LogEntry?> LogEntryProperty =
        AvaloniaProperty.Register<HighlightedLogTextBlock, LogEntry?>(nameof(LogEntry));

    public LogEntry? LogEntry
    {
        get => GetValue(LogEntryProperty);
        set => SetValue(LogEntryProperty, value);
    }

    static HighlightedLogTextBlock()
    {
        LogEntryProperty.Changed.AddClassHandler<HighlightedLogTextBlock>(OnLogEntryChanged);
    }

    private static void OnLogEntryChanged(HighlightedLogTextBlock sender, AvaloniaPropertyChangedEventArgs e)
    {
        sender.Inlines?.Clear();

        if (e.NewValue is LogEntry entry)
        {
            sender.Inlines ??= [];
            foreach (var run in LogMessageHighlighter.Highlight(entry.Message, entry.Level))
            {
                sender.Inlines.Add(run);
            }
        }
    }
}
