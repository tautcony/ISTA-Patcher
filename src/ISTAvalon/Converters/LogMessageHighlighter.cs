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

    [GeneratedRegex(@"\x1B\[(?<codes>[0-9;]*)m")]
    private static partial Regex AnsiSgrPattern();

    public static IReadOnlyList<Run> Highlight(string message, LogEventLevel level)
    {
        if (message.IndexOf('\u001b') >= 0)
        {
            return HighlightAnsi(message, level);
        }

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

    private static IReadOnlyList<Run> HighlightAnsi(string message, LogEventLevel level)
    {
        var levelBrush = GetLevelBrush(level);
        var result = new List<Run>();
        var currentBrush = levelBrush;
        var lastIndex = 0;

        foreach (Match match in AnsiSgrPattern().Matches(message))
        {
            if (match.Index > lastIndex)
            {
                result.Add(new Run(message[lastIndex..match.Index]) { Foreground = currentBrush });
            }

            var codes = match.Groups["codes"].Value;
            currentBrush = ApplyAnsiCodes(codes, currentBrush, levelBrush);
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < message.Length)
        {
            result.Add(new Run(message[lastIndex..]) { Foreground = currentBrush });
        }

        if (result.Count == 0)
        {
            result.Add(new Run(string.Empty) { Foreground = levelBrush });
        }

        return result;
    }

    private static IBrush ApplyAnsiCodes(string codes, IBrush current, IBrush levelBrush)
    {
        var brush = current;
        var parts = string.IsNullOrEmpty(codes) ? ["0"] : codes.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return levelBrush;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var code))
            {
                continue;
            }

            if (code == 38)
            {
                if (TryParseExtendedAnsiForeground(parts, ref i, out var extendedBrush))
                {
                    brush = extendedBrush;
                }

                continue;
            }

            brush = code switch
            {
                0 => levelBrush,
                39 => levelBrush,
                30 => LogPanelPalette.AnsiBlackBrush,
                31 => LogPanelPalette.AnsiRedBrush,
                32 => LogPanelPalette.AnsiGreenBrush,
                33 => LogPanelPalette.AnsiYellowBrush,
                34 => LogPanelPalette.AnsiBlueBrush,
                35 => LogPanelPalette.AnsiMagentaBrush,
                36 => LogPanelPalette.AnsiCyanBrush,
                37 => LogPanelPalette.AnsiWhiteBrush,
                90 => LogPanelPalette.AnsiBrightBlackBrush,
                91 => LogPanelPalette.AnsiBrightRedBrush,
                92 => LogPanelPalette.AnsiBrightGreenBrush,
                93 => LogPanelPalette.AnsiBrightYellowBrush,
                94 => LogPanelPalette.AnsiBrightBlueBrush,
                95 => LogPanelPalette.AnsiBrightMagentaBrush,
                96 => LogPanelPalette.AnsiBrightCyanBrush,
                97 => LogPanelPalette.AnsiBrightWhiteBrush,
                _ => brush,
            };
        }

        return brush;
    }

    private static bool TryParseExtendedAnsiForeground(string[] parts, ref int index, out IBrush brush)
    {
        brush = default!;

        if (index + 1 >= parts.Length || !int.TryParse(parts[index + 1], out var mode))
        {
            return false;
        }

        // 38;5;<n> (xterm 256 color)
        if (mode == 5 && index + 2 < parts.Length && int.TryParse(parts[index + 2], out var paletteIndex))
        {
            index += 2;
            brush = new SolidColorBrush(MapAnsi256ToColor(paletteIndex));
            return true;
        }

        // 38;2;<r>;<g>;<b> (true color)
        if (mode == 2 &&
            index + 4 < parts.Length &&
            int.TryParse(parts[index + 2], out var r) &&
            int.TryParse(parts[index + 3], out var g) &&
            int.TryParse(parts[index + 4], out var b))
        {
            index += 4;
            brush = new SolidColorBrush(Color.FromRgb((byte)ClampColor(r), (byte)ClampColor(g), (byte)ClampColor(b)));
            return true;
        }

        return false;
    }

    private static int ClampColor(int value)
    {
        return Math.Clamp(value, 0, 255);
    }

    private static Color MapAnsi256ToColor(int code)
    {
        code = Math.Clamp(code, 0, 255);

        if (code < 16)
        {
            // Standard + bright colors aligned to ANSI defaults.
            return code switch
            {
                0 => Color.Parse("#000000"),
                1 => Color.Parse("#800000"),
                2 => Color.Parse("#008000"),
                3 => Color.Parse("#808000"),
                4 => Color.Parse("#000080"),
                5 => Color.Parse("#800080"),
                6 => Color.Parse("#008080"),
                7 => Color.Parse("#c0c0c0"),
                8 => Color.Parse("#808080"),
                9 => Color.Parse("#ff0000"),
                10 => Color.Parse("#00ff00"),
                11 => Color.Parse("#ffff00"),
                12 => Color.Parse("#0000ff"),
                13 => Color.Parse("#ff00ff"),
                14 => Color.Parse("#00ffff"),
                _ => Color.Parse("#ffffff"),
            };
        }

        if (code <= 231)
        {
            var colorIndex = code - 16;
            var r = colorIndex / 36;
            var g = (colorIndex % 36) / 6;
            var b = colorIndex % 6;

            static byte Channel(int v) => v == 0 ? (byte)0 : (byte)(55 + (40 * v));
            return Color.FromRgb(Channel(r), Channel(g), Channel(b));
        }

        var gray = (byte)(8 + (code - 232) * 10);
        return Color.FromRgb(gray, gray, gray);
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
