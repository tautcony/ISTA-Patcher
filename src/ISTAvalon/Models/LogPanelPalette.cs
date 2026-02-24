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
    public static IBrush DebugBrush { get; set; } = new SolidColorBrush(Color.Parse("#9CA3AF"));     // gray-400
    public static IBrush InformationBrush { get; set; } = new SolidColorBrush(Color.Parse("#D1D5DB")); // gray-300
    public static IBrush WarningBrush { get; set; } = new SolidColorBrush(Color.Parse("#FBBF24"));   // amber-400
    public static IBrush ErrorBrush { get; set; } = new SolidColorBrush(Color.Parse("#EF4444"));     // red-500
    public static IBrush FatalBrush { get; set; } = new SolidColorBrush(Color.Parse("#DC2626"));     // red-600

    // ── Token foregrounds (inline syntax highlighting) ──────────────
    public static IBrush StringBrush { get; set; } = new SolidColorBrush(Color.Parse("#A5D6A7"));    // green-300
    public static IBrush NumberBrush { get; set; } = new SolidColorBrush(Color.Parse("#90CAF9"));    // blue-200

    // ── Panel chrome ────────────────────────────────────────────────
    public static IBrush TimestampBrush { get; set; } = new SolidColorBrush(Color.Parse("#6B7280")); // gray-500
    public static IBrush BackgroundBrush { get; set; } = new SolidColorBrush(Color.Parse("#1F2937")); // gray-800
    public static IBrush HeaderBrush { get; set; } = new SolidColorBrush(Color.Parse("#374151"));    // gray-700
}
