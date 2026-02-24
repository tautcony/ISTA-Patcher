// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Converters;

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ISTAvalon.Models;
using Serilog.Events;

/// <summary>
/// Converts a <see cref="LogEventLevel"/> to an <see cref="IBrush"/>
/// using colors from <see cref="LogPanelPalette"/>.
/// </summary>
public sealed class LogLevelToBrushConverter : IValueConverter
{
    public static LogLevelToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogEventLevel level)
        {
            return level switch
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

        return LogPanelPalette.InformationBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
