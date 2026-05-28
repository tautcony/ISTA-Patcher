// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAlter;
using ISTAvalon.Models;
using ISTAvalon.Services;
using ISTAvalon.ViewModels;

public class CommandExecutionServiceTests
{
    [SetUp]
    public void Setup()
    {
        ChildCommand.LastParent = null;
        ChildCommand.LastName = null;
        ChildCommand.LastTags = [];
        ChildCommand.LastPatchType = default;
    }

    [Test]
    public async Task ExecuteAsync_SetsParentAndCommandParametersBeforeInvokingRunAsync()
    {
        var parameters = new List<ParameterViewModel>
        {
            CreateParameter<ParentCommand>(nameof(ParentCommand.Verbose), ParameterKind.Bool, isParentOption: true, value: "true"),
            CreateParameter<ChildCommand>(nameof(ChildCommand.Name), ParameterKind.String, value: "tester"),
            CreateParameter<ChildCommand>(nameof(ChildCommand.Tags), ParameterKind.StringArray, value: "alpha, beta"),
            CreateParameter<ChildCommand>(
                nameof(ChildCommand.PatchType),
                ParameterKind.Enum,
                value: nameof(ISTAOptions.PatchType.Toyota),
                enumValues: Enum.GetNames<ISTAOptions.PatchType>()),
        };
        var descriptor = new CommandDescriptor
        {
            Name = "child",
            CommandType = typeof(ChildCommand),
            ParentCommandType = typeof(ParentCommand),
            Parameters = parameters.Select(p => p.Descriptor).ToList(),
        };

        var result = await CommandExecutionService.ExecuteAsync(descriptor, parameters);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(7));
            Assert.That(ChildCommand.LastParent?.Verbose, Is.True);
            Assert.That(ChildCommand.LastName, Is.EqualTo("tester"));
            Assert.That(ChildCommand.LastTags, Is.EqualTo(new[] { "alpha", "beta" }));
            Assert.That(ChildCommand.LastPatchType, Is.EqualTo(ISTAOptions.PatchType.Toyota));
        }
    }

    [Test]
    public void ExecuteAsync_ThrowsWhenRunAsyncIsMissing()
    {
        var descriptor = new CommandDescriptor
        {
            Name = "missing-run",
            CommandType = typeof(MissingRunCommand),
            Parameters = [],
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            CommandExecutionService.ExecuteAsync(descriptor, []));

        Assert.That(ex!.Message, Does.Contain(nameof(MissingRunCommand)));
    }

    private static ParameterViewModel CreateParameter<TCommand>(
        string propertyName,
        ParameterKind kind,
        bool isParentOption = false,
        string? value = null,
        string[]? enumValues = null)
    {
        var property = typeof(TCommand).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Missing property {propertyName}.");

        var descriptor = new ParameterDescriptor
        {
            Name = propertyName,
            DisplayName = propertyName,
            Kind = kind,
            PropertyType = property.PropertyType,
            IsParentOption = isParentOption,
            EnumValues = enumValues ?? [],
            PropertyInfo = property,
        };

        var vm = ParameterViewModel.Create(descriptor);
        if (value is not null)
        {
            vm.ApplyValue(value);
        }

        return vm;
    }

    public sealed class ParentCommand
    {
        public bool Verbose { get; set; }
    }

    public sealed class ChildCommand
    {
        public static ParentCommand? LastParent { get; set; }

        public static string? LastName { get; set; }

        public static string[] LastTags { get; set; } = [];

        public static ISTAOptions.PatchType LastPatchType { get; set; }

        public ParentCommand? ParentCommand { get; set; }

        public string? Name { get; set; }

        public string[] Tags { get; set; } = [];

        public ISTAOptions.PatchType PatchType { get; set; }

        public Task<int> RunAsync()
        {
            LastParent = ParentCommand;
            LastName = Name;
            LastTags = Tags;
            LastPatchType = PatchType;
            return Task.FromResult(7);
        }
    }

    public sealed class MissingRunCommand
    {
        public string? Name { get; set; }
    }
}
