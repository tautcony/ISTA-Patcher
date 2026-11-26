// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using ISTAvalon.Models;

public abstract class ParameterViewModel(ParameterDescriptor descriptor) : ObservableObject
{
    public ParameterDescriptor Descriptor { get; } = descriptor;

    public abstract bool HasValue { get; }

    /// <summary>Human-readable label shown in the UI. Falls back to DisplayName when no description is available.</summary>
    public string LabelText =>
        string.IsNullOrEmpty(Descriptor.Description) ? Descriptor.DisplayName : Descriptor.Description;

    /// <summary>CLI option/argument name shown in the tooltip (e.g. --patch-type or &lt;target-path&gt;).</summary>
    public string TooltipText => Descriptor.CliOption;

    public abstract object? GetValue();

    public virtual void ApplyValue(string value) { }

    public static ParameterViewModel Create(ParameterDescriptor descriptor)
    {
        return descriptor.Kind switch
        {
            ParameterKind.Bool => new BoolParameterViewModel(descriptor),
            ParameterKind.Enum => new EnumParameterViewModel(descriptor),
            ParameterKind.Integer => new NumericParameterViewModel(descriptor),
            ParameterKind.Float => new NumericParameterViewModel(descriptor),
            ParameterKind.Numeric => new NumericParameterViewModel(descriptor),
            ParameterKind.Path => new PathParameterViewModel(descriptor),
            ParameterKind.String => new StringParameterViewModel(descriptor),
            ParameterKind.StringArray => new StringArrayParameterViewModel(descriptor),
            _ => new StringParameterViewModel(descriptor),
        };
    }
}

public class BoolParameterViewModel(ParameterDescriptor descriptor) : ParameterViewModel(descriptor)
{
    private bool _isChecked = descriptor.DefaultValue is true;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public override bool HasValue => true;

    public override object? GetValue() => IsChecked;

    public override void ApplyValue(string value)
    {
        if (bool.TryParse(value, out var b))
        {
            IsChecked = b;
        }
    }
}

public class EnumParameterViewModel(ParameterDescriptor descriptor) : ParameterViewModel(descriptor)
{
    private string? _selectedValue = descriptor.DefaultValue as string;

    public string[] EnumValues => Descriptor.EnumValues;

    public string? SelectedValue
    {
        get => _selectedValue;
        set => SetProperty(ref _selectedValue, value);
    }

    public override bool HasValue => SelectedValue != null;

    public override object? GetValue() =>
        SelectedValue != null ? Enum.Parse(Descriptor.PropertyType, SelectedValue) : null;

    public override void ApplyValue(string value)
    {
        if (Descriptor.EnumValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            SelectedValue = Descriptor.EnumValues.First(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
        }
    }
}

public class NumericParameterViewModel : ParameterViewModel
{
    private decimal _numericValue;
    private string _numericText = string.Empty;
    private readonly bool _isIntegerKind;

    public decimal Increment => _isIntegerKind ? 1m : 0.1m;

    public string FormatString => _isIntegerKind ? "N0" : "0.###";

    public decimal NumericValue
    {
        get => _numericValue;
        set
        {
            var normalized = _isIntegerKind ? decimal.Truncate(value) : value;
            if (SetProperty(ref _numericValue, normalized))
            {
                SetProperty(ref _numericText, normalized.ToString(this.FormatString, CultureInfo.CurrentCulture), nameof(this.NumericText));
            }
        }
    }

    public string NumericText
    {
        get => this._numericText;
        set
        {
            if (!this.SetProperty(ref this._numericText, value))
            {
                return;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
            {
                this.NumericValue = parsed;
            }
        }
    }

    public NumericParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        _isIntegerKind = descriptor.Kind == ParameterKind.Integer;

        try
        {
            _numericValue = descriptor.DefaultValue != null ? Convert.ToDecimal(descriptor.DefaultValue) : 0m;
        }
        catch
        {
            _numericValue = 0m;
        }

        _numericText = _numericValue.ToString(this.FormatString, CultureInfo.CurrentCulture);
    }

    public override bool HasValue => true;

    public override object? GetValue() => Convert.ChangeType(NumericValue, Descriptor.PropertyType);

    public override void ApplyValue(string value)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var d))
        {
            NumericValue = d;
        }
    }
}

public class StringParameterViewModel(ParameterDescriptor descriptor) : ParameterViewModel(descriptor)
{
    private string? _textValue = descriptor.DefaultValue as string;

    public string? TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public override bool HasValue => !string.IsNullOrWhiteSpace(TextValue);

    public override object? GetValue() => TextValue;

    public override void ApplyValue(string value) => TextValue = value;
}

public class PathParameterViewModel(ParameterDescriptor descriptor) : ParameterViewModel(descriptor)
{
    private string? _textValue = descriptor.DefaultValue as string;

    public string? TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public override bool HasValue => !string.IsNullOrWhiteSpace(TextValue);

    public override object? GetValue() => TextValue;

    public override void ApplyValue(string value) => TextValue = value;
}

public class StringArrayParameterViewModel : ParameterViewModel
{
    private string? _textValue;

    public string? TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public StringArrayParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        _textValue = descriptor.DefaultValue is string[] arr ? string.Join(", ", arr) : null;
    }

    public override bool HasValue => !string.IsNullOrWhiteSpace(TextValue);

    public override object? GetValue() =>
        TextValue?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    public override void ApplyValue(string value) => TextValue = value;
}
