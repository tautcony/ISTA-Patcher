// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Models;

using Avalonia.Media;

/// <summary>
/// Centralised color palette for the log panel.
/// All log-panel consumers read brush values from this class.
/// Modify properties here to re-skin the entire log panel.
/// </summary>
public static class LogPanelPalette
{
    // ── Per-level foregrounds ───────────────────────────────────────
    public static IBrush VerboseBrush { get; set; } = new SolidColorBrush(Color.Parse("#6B7280"));   // gray-500
    public static IBrush DebugBrush { get; set; } = new SolidColorBrush(Color.Parse("#374151"));     // gray-700
    public static IBrush InformationBrush { get; set; } = new SolidColorBrush(Color.Parse("#1F2937")); // gray-800
    public static IBrush WarningBrush { get; set; } = new SolidColorBrush(Color.Parse("#FBBF24"));   // amber-400
    public static IBrush ErrorBrush { get; set; } = new SolidColorBrush(Color.Parse("#EF4444"));     // red-500
    public static IBrush FatalBrush { get; set; } = new SolidColorBrush(Color.Parse("#DC2626"));     // red-600

    // ── Token foregrounds (inline syntax highlighting) ──────────────
    public static IBrush StringBrush { get; set; } = new SolidColorBrush(Color.Parse("#A5D6A7"));    // green-300
    public static IBrush NumberBrush { get; set; } = new SolidColorBrush(Color.Parse("#90CAF9"));    // blue-200

    // ── ANSI foregrounds (SGR 30-37, 90-97) ────────────────────────
    public static IBrush AnsiBlackBrush { get; set; } = new SolidColorBrush(Color.Parse("#111827"));
    public static IBrush AnsiRedBrush { get; set; } = new SolidColorBrush(Color.Parse("#EF4444"));
    public static IBrush AnsiGreenBrush { get; set; } = new SolidColorBrush(Color.Parse("#22C55E"));
    public static IBrush AnsiYellowBrush { get; set; } = new SolidColorBrush(Color.Parse("#EAB308"));
    public static IBrush AnsiBlueBrush { get; set; } = new SolidColorBrush(Color.Parse("#3B82F6"));
    public static IBrush AnsiMagentaBrush { get; set; } = new SolidColorBrush(Color.Parse("#D946EF"));
    public static IBrush AnsiCyanBrush { get; set; } = new SolidColorBrush(Color.Parse("#06B6D4"));
    public static IBrush AnsiWhiteBrush { get; set; } = new SolidColorBrush(Color.Parse("#E5E7EB"));

    public static IBrush AnsiBrightBlackBrush { get; set; } = new SolidColorBrush(Color.Parse("#6B7280"));
    public static IBrush AnsiBrightRedBrush { get; set; } = new SolidColorBrush(Color.Parse("#F87171"));
    public static IBrush AnsiBrightGreenBrush { get; set; } = new SolidColorBrush(Color.Parse("#4ADE80"));
    public static IBrush AnsiBrightYellowBrush { get; set; } = new SolidColorBrush(Color.Parse("#FACC15"));
    public static IBrush AnsiBrightBlueBrush { get; set; } = new SolidColorBrush(Color.Parse("#60A5FA"));
    public static IBrush AnsiBrightMagentaBrush { get; set; } = new SolidColorBrush(Color.Parse("#E879F9"));
    public static IBrush AnsiBrightCyanBrush { get; set; } = new SolidColorBrush(Color.Parse("#22D3EE"));
    public static IBrush AnsiBrightWhiteBrush { get; set; } = new SolidColorBrush(Color.Parse("#F9FAFB"));

    // ── Panel chrome ────────────────────────────────────────────────
    public static IBrush TimestampBrush { get; set; } = new SolidColorBrush(Color.Parse("#6B7280")); // gray-500
    public static IBrush BackgroundBrush { get; set; } = new SolidColorBrush(Color.Parse("#1F2937")); // gray-800
    public static IBrush HeaderBrush { get; set; } = new SolidColorBrush(Color.Parse("#374151"));    // gray-700
}
