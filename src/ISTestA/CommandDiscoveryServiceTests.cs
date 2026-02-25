// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using DotMake.CommandLine;
using ISTAvalon.Services;
using ISTAvalon.ViewModels;

public class CommandDiscoveryServiceTests
{
    [Test]
    public void DiscoverCommands_DefaultFilter_ExcludesHiddenCommands()
    {
        var commands = CommandDiscoveryService.DiscoverCommands(includeHidden: false);

        Assert.That(FlattenNames(commands), Does.Not.Contain("cerebrumancy"));
    }

    [Test]
    public void DiscoverCommands_IncludeHidden_IncludesHiddenCommands()
    {
        var commands = CommandDiscoveryService.DiscoverCommands(includeHidden: true);

        Assert.That(FlattenNames(commands), Does.Contain("cerebrumancy"));
    }

    [Test]
    public void DiscoverCommands_UsesNestedParentOverExplicitParent()
    {
        var commands = CommandDiscoveryService.DiscoverCommands([
            typeof(OuterCommand),
            typeof(OuterCommand.NestedChildCommand),
            typeof(StandaloneRootCommand),
        ]);

        var outer = commands.Single(c => c.Name == "outer");
        var standalone = commands.Single(c => c.Name == "standalone");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outer.Subcommands.Select(c => c.Name), Does.Contain("nested-child"));
            Assert.That(standalone.Subcommands.Select(c => c.Name), Does.Not.Contain("nested-child"));
        }
    }

    [Test]
    public void DiscoverCommands_WithMissingParent_KeepsCommandAsRoot()
    {
        var commands = CommandDiscoveryService.DiscoverCommands([
            typeof(OrphanCommand),
        ]);

        Assert.That(commands.Select(c => c.Name).ToArray(), Is.EqualTo(["orphan"]));
    }

    [Test]
    public void DiscoverCommands_MergesInheritedAndParentParametersWithOverrides()
    {
        var commands = CommandDiscoveryService.DiscoverCommands([
            typeof(ParentCommand),
            typeof(DerivedOptionCommand),
        ]);

        var parent = commands.Single(c => c.Name == "parent");
        var child = parent.Subcommands.Single(c => c.Name == "derived-option");

        var parentOption = child.Parameters.Single(p => p.Name == nameof(ParentCommand.Verbosity));
        Assert.That(parentOption.IsParentOption, Is.True);

        var sharedOption = child.Parameters.Single(p => p.Name == nameof(DerivedOptionCommand.Shared));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sharedOption.IsParentOption, Is.False);
            Assert.That(sharedOption.PropertyInfo.DeclaringType, Is.EqualTo(typeof(DerivedOptionCommand)));

            Assert.That(child.Parameters.Count(p => p.Name == nameof(DerivedOptionCommand.Shared)), Is.EqualTo(1));
        }
    }

    [Test]
    public void MainWindowViewModel_OnlyUsesRootCommandsAsTabsAndSupportsSubcommandSelection()
    {
        var commands = CommandDiscoveryService.DiscoverCommands([
            typeof(ParentCommand),
            typeof(DerivedOptionCommand),
        ]);

        var vm = new MainWindowViewModel(commands);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CommandTabs.Select(t => t.Name).ToArray(), Is.EqualTo(["parent"]));
            Assert.That(vm.CommandTabs[0].HasSubcommands, Is.True);
            Assert.That(vm.CommandTabs[0].AvailableCommands.Select(c => c.Name), Is.EqualTo(["parent", "derived-option"
            ]));
        }

        vm.CommandTabs[0].SelectedCommand = vm.CommandTabs[0].AvailableCommands.Single(c => c.Name == "derived-option");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CommandTabs[0].Parameters.Any(p => p.Descriptor.Name == nameof(DerivedOptionCommand.LocalValue)), Is.True);
            Assert.That(vm.CommandTabs[0].Parameters.Any(p => p.Descriptor.Name == nameof(DerivedOptionCommand.Shared)), Is.True);
        }
    }

    [CliCommand(Name = "standalone")]
    internal class StandaloneRootCommand
    {
        public Task RunAsync() => Task.CompletedTask;
    }

    [CliCommand(Name = "outer")]
    internal class OuterCommand
    {
        public Task RunAsync() => Task.CompletedTask;

        [CliCommand(Name = "nested-child", Parent = typeof(StandaloneRootCommand))]
        public class NestedChildCommand
        {
            public Task RunAsync() => Task.CompletedTask;
        }
    }

    [CliCommand(Name = "orphan", Parent = typeof(UnknownParentCommand))]
    internal class OrphanCommand
    {
        public Task RunAsync() => Task.CompletedTask;
    }

    [CliCommand(Name = "unknown-parent")]
    internal class UnknownParentCommand
    {
        public Task RunAsync() => Task.CompletedTask;
    }

    [CliCommand(Name = "parent")]
    internal class ParentCommand
    {
        [CliOption]
        public bool Verbosity { get; set; }

        public Task RunAsync() => Task.CompletedTask;
    }

    internal class BaseOptionCommand
    {
        [CliOption]
        public string? Shared { get; set; }
    }

    [CliCommand(Name = "derived-option", Parent = typeof(ParentCommand))]
    internal class DerivedOptionCommand : BaseOptionCommand
    {
        [CliOption]
        public new string? Shared { get; set; }

        [CliOption(Required = true)]
        public string? LocalValue { get; set; }

        public Task RunAsync() => Task.CompletedTask;
    }

    private static List<string> FlattenNames(IEnumerable<ISTAvalon.Models.CommandDescriptor> roots)
    {
        var names = new List<string>();
        foreach (var root in roots)
        {
            Add(root, names);
        }

        return names;

        static void Add(ISTAvalon.Models.CommandDescriptor descriptor, ICollection<string> output)
        {
            output.Add(descriptor.Name);
            foreach (var subcommand in descriptor.Subcommands)
            {
                Add(subcommand, output);
            }
        }
    }
}
