// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using System.Globalization;
using Avalonia.Controls;
using ISTAvalon.Converters;
using ISTAvalon.Models;
using Serilog.Events;

public class ConverterAndMetadataTests
{
    [TestCase(true, 1d, GridUnitType.Star)]
    [TestCase(false, 0d, GridUnitType.Pixel)]
    [TestCase(null, 0d, GridUnitType.Pixel)]
    public void BoolToGridLengthConverter_MapsTrueToStarAndOtherValuesToZero(object? value, double expectedValue, GridUnitType expectedUnit)
    {
        var result = (GridLength)BoolToGridLengthConverter.Instance.Convert(value, typeof(GridLength), null, CultureInfo.InvariantCulture);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value, Is.EqualTo(expectedValue));
            Assert.That(result.GridUnitType, Is.EqualTo(expectedUnit));
        }
    }

    [Test]
    public void BoolToGridLengthConverter_ConvertBackIsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            BoolToGridLengthConverter.Instance.ConvertBack(new GridLength(1), typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [TestCase(LogEventLevel.Verbose, nameof(LogPanelPalette.VerboseBrush))]
    [TestCase(LogEventLevel.Debug, nameof(LogPanelPalette.DebugBrush))]
    [TestCase(LogEventLevel.Information, nameof(LogPanelPalette.InformationBrush))]
    [TestCase(LogEventLevel.Warning, nameof(LogPanelPalette.WarningBrush))]
    [TestCase(LogEventLevel.Error, nameof(LogPanelPalette.ErrorBrush))]
    [TestCase(LogEventLevel.Fatal, nameof(LogPanelPalette.FatalBrush))]
    public void LogLevelToBrushConverter_MapsEachLevelToPaletteBrush(LogEventLevel level, string palettePropertyName)
    {
        var expected = typeof(LogPanelPalette).GetProperty(palettePropertyName)!.GetValue(null);
        var result = LogLevelToBrushConverter.Instance.Convert(level, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.SameAs(expected));
    }

    [Test]
    public void LogLevelToBrushConverter_FallsBackToInformationBrush()
    {
        var result = LogLevelToBrushConverter.Instance.Convert("not-a-level", typeof(object), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.SameAs(LogPanelPalette.InformationBrush));
    }

    [Test]
    public void LogLevelToBrushConverter_ConvertBackIsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            LogLevelToBrushConverter.Instance.ConvertBack(LogPanelPalette.InformationBrush, typeof(LogEventLevel), null, CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppMetadata_WindowTitle_CombinesProductNameAndVersion()
    {
        var metadata = new AppMetadata(
            "ISTA-Patcher",
            "2.5.0",
            "tautcony/ISTA-Patcher",
            "https://github.com/tautcony/ISTA-Patcher",
            "GPL-3.0-or-later");

        Assert.That(metadata.WindowTitle, Is.EqualTo("ISTA-Patcher v2.5.0"));
    }
}
