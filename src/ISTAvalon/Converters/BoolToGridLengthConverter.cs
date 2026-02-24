// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

/// <summary>
/// Converts a boolean to a <see cref="GridLength"/>.
/// <c>true</c> → <c>*</c> (star), <c>false</c> → <c>0</c> (collapsed).
/// </summary>
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public static BoolToGridLengthConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
