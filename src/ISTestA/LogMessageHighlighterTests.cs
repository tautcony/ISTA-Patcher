// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using Avalonia.Media;
using ISTAvalon.Converters;
using ISTAvalon.Models;
using Serilog.Events;

public class LogMessageHighlighterTests
{
    [Test]
    public void Highlight_AnsiRedAndReset_RendersWithoutEscapeCodes()
    {
        const string message = "\u001b[31mERR\u001b[0m normal";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("ERR normal"));
        Assert.That(runs[0].Foreground, Is.SameAs(LogPanelPalette.AnsiRedBrush));
        Assert.That(runs[^1].Foreground, Is.SameAs(LogPanelPalette.InformationBrush));
    }

    [Test]
    public void Highlight_AnsiBrightYellowAndDefaultReset_AppliesExpectedBrushes()
    {
        const string message = "\u001b[93mWARN\u001b[39m done";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Warning);

        Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("WARN done"));
        Assert.That(runs[0].Foreground, Is.SameAs(LogPanelPalette.AnsiBrightYellowBrush));
        Assert.That(runs[^1].Foreground, Is.SameAs(LogPanelPalette.WarningBrush));
    }

    [Test]
    public void Highlight_AnsiTrueColorForeground_AppliesRgbBrush()
    {
        const string message = "\u001b[38;2;12;200;34mRGB\u001b[0m";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("RGB"));
        Assert.That(runs[0].Foreground, Is.TypeOf<SolidColorBrush>());
        var brush = (SolidColorBrush)runs[0].Foreground!;
        Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(12, 200, 34)));
    }

    [Test]
    public void Highlight_Ansi256Foreground_AppliesPaletteBrush()
    {
        const string message = "\u001b[38;5;196mALERT\u001b[0m";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("ALERT"));
        Assert.That(runs[0].Foreground, Is.TypeOf<SolidColorBrush>());
        var brush = (SolidColorBrush)runs[0].Foreground!;
        Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(255, 0, 0)));
    }
}
