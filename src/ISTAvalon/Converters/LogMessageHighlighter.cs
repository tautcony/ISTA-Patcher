// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Converters;

using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using ISTAvalon.Models;
using Serilog.Events;

/// <summary>
/// Tokenises a log message string into colored <see cref="Run"/> elements.
/// Recognises quoted strings and numeric literals; everything else gets the
/// level-based foreground color.
/// </summary>
public static partial class LogMessageHighlighter
{
    // Matches: "quoted" | 'quoted' | standalone numbers (int/decimal)
    [GeneratedRegex("""(?<str>"[^"]*"|'[^']*')|(?<num>\b\d+(?:\.\d+)?\b)""")]
    private static partial Regex TokenPattern();

    public static IReadOnlyList<Run> Highlight(string message, LogEventLevel level)
    {
        var levelBrush = GetLevelBrush(level);
        var result = new List<Run>();
        var lastIndex = 0;

        foreach (Match match in TokenPattern().Matches(message))
        {
            // Plain text before this match
            if (match.Index > lastIndex)
            {
                result.Add(new Run(message[lastIndex..match.Index]) { Foreground = levelBrush });
            }

            var segment = match.Value;

            // Determine token type by examining the first character
            if (segment.Length > 0 && (segment[0] == '"' || segment[0] == '\''))
            {
                // Strip surrounding quotes — show only the inner content in string color
                var inner = segment.Length >= 2 ? segment[1..^1] : segment;
                result.Add(new Run(inner) { Foreground = LogPanelPalette.StringBrush });
            }
            else
            {
                result.Add(new Run(segment) { Foreground = LogPanelPalette.NumberBrush });
            }

            lastIndex = match.Index + match.Length;
        }

        // Trailing plain text
        if (lastIndex < message.Length)
        {
            result.Add(new Run(message[lastIndex..]) { Foreground = levelBrush });
        }

        // If message was empty, add at least one empty run
        if (result.Count == 0)
        {
            result.Add(new Run(message) { Foreground = levelBrush });
        }

        return result;
    }

    private static IBrush GetLevelBrush(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogPanelPalette.VerboseBrush,
        LogEventLevel.Debug => LogPanelPalette.DebugBrush,
        LogEventLevel.Information => LogPanelPalette.InformationBrush,
        LogEventLevel.Warning => LogPanelPalette.WarningBrush,
        LogEventLevel.Error => LogPanelPalette.ErrorBrush,
        LogEventLevel.Fatal => LogPanelPalette.FatalBrush,
        _ => LogPanelPalette.InformationBrush,
    };
}
