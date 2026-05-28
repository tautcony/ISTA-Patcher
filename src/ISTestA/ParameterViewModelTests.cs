// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAlter;
using ISTAvalon.Models;
using ISTAvalon.ViewModels;

public class ParameterViewModelTests
{
    [Test]
    public void Create_ReturnsExpectedViewModelType_ForEachParameterKind()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Enabled), ParameterKind.Bool)), Is.TypeOf<BoolParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Mode), ParameterKind.Enum)), Is.TypeOf<EnumParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Count), ParameterKind.Integer)), Is.TypeOf<NumericParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Ratio), ParameterKind.Float)), Is.TypeOf<NumericParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Path), ParameterKind.Path)), Is.TypeOf<PathParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Text), ParameterKind.String)), Is.TypeOf<StringParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Tags), ParameterKind.StringArray)), Is.TypeOf<StringArrayParameterViewModel>());
            Assert.That(ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Text), ParameterKind.Fallback)), Is.TypeOf<StringParameterViewModel>());
        }
    }

    [Test]
    public void CommonText_UsesDescriptionFallbackAndCliOption()
    {
        var withDescription = ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.Text),
            ParameterKind.String,
            displayName: "Text",
            description: "Text value",
            cliOption: "--text"));
        var withoutDescription = ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.Path),
            ParameterKind.Path,
            displayName: "Path",
            cliOption: "<path>"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withDescription.LabelText, Is.EqualTo("Text value"));
            Assert.That(withDescription.TooltipText, Is.EqualTo("--text"));
            Assert.That(withoutDescription.LabelText, Is.EqualTo("Path"));
            Assert.That(withoutDescription.TooltipText, Is.EqualTo("<path>"));
        }
    }

    [TestCase("true", true)]
    [TestCase("false", false)]
    [TestCase("not-a-bool", true)]
    public void BoolParameterViewModel_ParsesBooleanValues(string input, bool expected)
    {
        var vm = (BoolParameterViewModel)ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.Enabled),
            ParameterKind.Bool,
            defaultValue: true));

        vm.ApplyValue(input);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.HasValue, Is.True);
            Assert.That(vm.GetValue(), Is.EqualTo(expected));
        }
    }

    [Test]
    public void EnumParameterViewModel_AppliesCaseInsensitivePreset()
    {
        var vm = (EnumParameterViewModel)ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.PatchType),
            ParameterKind.Enum,
            defaultValue: nameof(ISTAOptions.PatchType.BMW),
            enumValues: Enum.GetNames<ISTAOptions.PatchType>()));

        vm.ApplyValue("toyota");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.HasValue, Is.True);
            Assert.That(vm.SelectedValue, Is.EqualTo(nameof(ISTAOptions.PatchType.Toyota)));
            Assert.That(vm.GetValue(), Is.EqualTo(ISTAOptions.PatchType.Toyota));
        }
    }

    [Test]
    public void NumericParameterViewModel_TruncatesIntegerAndPreservesFloatValues()
    {
        var intVm = (NumericParameterViewModel)ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.Count),
            ParameterKind.Integer,
            defaultValue: "invalid"));
        var floatVm = (NumericParameterViewModel)ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.Ratio),
            ParameterKind.Float,
            defaultValue: 1.25d));

        intVm.ApplyValue("42.9");
        floatVm.ApplyValue("3.5");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(intVm.Increment, Is.EqualTo(1m));
            Assert.That(intVm.FormatString, Is.EqualTo("N0"));
            Assert.That(intVm.GetValue(), Is.EqualTo(42));
            Assert.That(floatVm.Increment, Is.EqualTo(0.1m));
            Assert.That(floatVm.FormatString, Is.EqualTo("0.###"));
            Assert.That(floatVm.GetValue(), Is.EqualTo(3.5d));
        }
    }

    [Test]
    public void StringAndPathParameterViewModels_ReportValuePresence()
    {
        var stringVm = (StringParameterViewModel)ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Text), ParameterKind.String));
        var pathVm = (PathParameterViewModel)ParameterViewModel.Create(CreateDescriptor(nameof(SampleOptions.Path), ParameterKind.Path));

        stringVm.ApplyValue("hello");
        pathVm.ApplyValue("/tmp/example");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stringVm.HasValue, Is.True);
            Assert.That(stringVm.GetValue(), Is.EqualTo("hello"));
            Assert.That(pathVm.HasValue, Is.True);
            Assert.That(pathVm.GetValue(), Is.EqualTo("/tmp/example"));
        }
    }

    [Test]
    public void StringArrayParameterViewModel_SplitsAndTrimsCommaSeparatedValues()
    {
        var vm = (StringArrayParameterViewModel)ParameterViewModel.Create(CreateDescriptor(
            nameof(SampleOptions.Tags),
            ParameterKind.StringArray,
            defaultValue: new[] { "default-a", "default-b" }));

        Assert.That(vm.GetValue(), Is.EqualTo(new[] { "default-a", "default-b" }));

        vm.ApplyValue("alpha, beta,,gamma ");

        Assert.That(vm.GetValue(), Is.EqualTo(new[] { "alpha", "beta", "gamma" }));
    }

    private static ParameterDescriptor CreateDescriptor(
        string propertyName,
        ParameterKind kind,
        string? displayName = null,
        string description = "",
        string cliOption = "",
        object? defaultValue = null,
        string[]? enumValues = null)
    {
        var property = typeof(SampleOptions).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Missing property {propertyName}.");

        return new ParameterDescriptor
        {
            Name = propertyName,
            DisplayName = displayName ?? propertyName,
            Description = description,
            Kind = kind,
            PropertyType = property.PropertyType,
            DefaultValue = defaultValue,
            EnumValues = enumValues ?? [],
            CliOption = cliOption,
            PropertyInfo = property,
        };
    }

    private sealed class SampleOptions
    {
        public bool Enabled { get; set; }

        public ISTAOptions.ModeType Mode { get; set; }

        public ISTAOptions.PatchType PatchType { get; set; }

        public int Count { get; set; }

        public double Ratio { get; set; }

        public string? Text { get; set; }

        public string? Path { get; set; }

        public string[] Tags { get; set; } = [];
    }
}
