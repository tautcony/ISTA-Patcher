// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using ISTAvalon.Models;

public abstract class ParameterViewModel(ParameterDescriptor descriptor) : ObservableObject
{
    public ParameterDescriptor Descriptor { get; } = descriptor;

    public abstract bool HasValue { get; }

    public abstract object? GetValue();

    public static ParameterViewModel Create(ParameterDescriptor descriptor)
    {
        return descriptor.Kind switch
        {
            ParameterKind.Bool => new BoolParameterViewModel(descriptor),
            ParameterKind.Enum => new EnumParameterViewModel(descriptor),
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
}

public class NumericParameterViewModel : ParameterViewModel
{
    private decimal _numericValue;

    public decimal NumericValue
    {
        get => _numericValue;
        set => SetProperty(ref _numericValue, value);
    }

    public NumericParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        try
        {
            _numericValue = descriptor.DefaultValue != null ? Convert.ToDecimal(descriptor.DefaultValue) : 0m;
        }
        catch
        {
            _numericValue = 0m;
        }
    }

    public override bool HasValue => true;

    public override object? GetValue() => Convert.ChangeType(NumericValue, Descriptor.PropertyType);
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
}
