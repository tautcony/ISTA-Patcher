// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using ISTAvalon.Models;

public abstract class ParameterViewModel : ObservableObject
{
    public ParameterDescriptor Descriptor { get; }

    protected ParameterViewModel(ParameterDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

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

public class BoolParameterViewModel : ParameterViewModel
{
    private bool _isChecked;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public BoolParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        _isChecked = descriptor.DefaultValue is true;
    }

    public override object? GetValue() => IsChecked;
}

public class EnumParameterViewModel : ParameterViewModel
{
    private string? _selectedValue;

    public string[] EnumValues => Descriptor.EnumValues;

    public string? SelectedValue
    {
        get => _selectedValue;
        set => SetProperty(ref _selectedValue, value);
    }

    public EnumParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        _selectedValue = descriptor.DefaultValue as string;
    }

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

    public override object? GetValue() => Convert.ChangeType(NumericValue, Descriptor.PropertyType);
}

public class StringParameterViewModel : ParameterViewModel
{
    private string? _textValue;

    public string? TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public StringParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        _textValue = descriptor.DefaultValue as string;
    }

    public override object? GetValue() => TextValue;
}

public class PathParameterViewModel : ParameterViewModel
{
    private string? _textValue;

    public string? TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public PathParameterViewModel(ParameterDescriptor descriptor) : base(descriptor)
    {
        _textValue = descriptor.DefaultValue as string;
    }

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

    public override object? GetValue() =>
        TextValue?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
}
