// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using System.Globalization;

namespace ISTAvalon.Services;

using ISTAvalon.Models;
using ISTAvalon.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Serializes the GUI ViewModel state as a structured YAML document.
/// </summary>
public static class GuiStateDumper
{
    private const int MaxLogEntries = 200;

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public static string DumpYaml(MainWindowViewModel vm)
    {
        var model = new WindowDump
        {
            Title = vm.WindowTitle,
            SelectedTab = vm.SelectedTab?.Name ?? string.Empty,
            Theme = BuildThemeDump(vm),
            Tabs = [.. vm.CommandTabs.Select(BuildTabDump)],
        };
        return YamlSerializer.Serialize(model);
    }

    private static ThemeDump BuildThemeDump(MainWindowViewModel vm) => new()
    {
        Current = vm.CurrentTheme.ToString(),
        ToggleButton = new ThemeToggleButtonDump
        {
            Role = "theme_toggle",
            Icon = vm.ThemeIcon,
            Tooltip = vm.ThemeTooltip,
            Command = nameof(vm.ToggleThemeCommand),
        },
    };

    private static TabDump BuildTabDump(CommandTabViewModel tab) => new()
    {
        Name = tab.Name,
        Description = tab.Description,
        SelectedCommand = tab.SelectedCommand?.Name ?? string.Empty,
        AvailableCommands = [.. tab.AvailableCommands.Select(c => c.Name)],
        HasSubcommands = tab.HasSubcommands,
        IsExecuting = tab.IsExecuting,
        HasPreset = tab.HasPreset,
        IsLogPanelExpanded = tab.IsLogPanelExpanded,
        CancelButton = new CommandControlDump
        {
            Role = "button",
            Icon = "fa-solid fa-stop",
            Tooltip = "Cancel running command",
            Command = nameof(tab.CancelCommand),
            IsVisible = tab.IsExecuting,
        },
        LogPanel = BuildLogPanelDump(tab),
        Controls = [.. tab.Parameters.Select(p => BuildParameterEditorDump(tab, p))],
        Status = tab.StatusText,
    };

    private static LogPanelDump BuildLogPanelDump(CommandTabViewModel tab)
    {
        var totalEntries = tab.OutputLines.Count;
        var skipped = Math.Max(0, totalEntries - MaxLogEntries);

        return new LogPanelDump
        {
            Role = "log_panel",
            IsExpanded = tab.IsLogPanelExpanded,
            Header = "Log",
            WidthColumn = tab.IsLogPanelExpanded ? "*" : "0",
            Controls = new LogPanelControlsDump
            {
                CopyAll = new CommandControlDump
                {
                    Role = "button",
                    Icon = "fa-regular fa-copy",
                    Tooltip = "Copy All",
                    Command = nameof(tab.CopyAllCommand),
                    IsVisible = tab.IsLogPanelExpanded,
                },
                Clear = new CommandControlDump
                {
                    Role = "button",
                    Icon = "fa-solid fa-trash-can",
                    Tooltip = "Clear",
                    Command = nameof(tab.ClearOutputCommand),
                    IsVisible = tab.IsLogPanelExpanded,
                },
                Collapse = new CommandControlDump
                {
                    Role = "button",
                    Icon = "fa-solid fa-angles-right",
                    Tooltip = "Collapse",
                    Command = nameof(tab.ToggleLogPanelCommand),
                    IsVisible = tab.IsLogPanelExpanded,
                },
                Expand = new CommandControlDump
                {
                    Role = "button",
                    Icon = "fa-solid fa-angles-left",
                    Tooltip = "Expand Log",
                    Command = nameof(tab.ToggleLogPanelCommand),
                    IsVisible = !tab.IsLogPanelExpanded,
                },
            },
            Entries =
            [
                .. tab.OutputLines
                    .Skip(skipped)
                    .Select(BuildLogEntryDump)
            ],
            TotalEntries = totalEntries,
            OmittedEntries = skipped,
        };
    }

    private static LogEntryDump BuildLogEntryDump(LogEntry entry) => new()
    {
        Timestamp = entry.Timestamp.ToString("O"),
        DisplayTimestamp = entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
        Level = entry.Level.ToString(),
        Message = GuiDumpRedaction.RedactLogMessage(entry.Message),
    };

    private static string ResolveComponent(ParameterViewModel param) => param switch
    {
        BoolParameterViewModel => "CheckBox",
        EnumParameterViewModel => "ComboBox",
        NumericParameterViewModel => "TextBox[numeric]",
        PathParameterViewModel => "PathEditor",
        StringArrayParameterViewModel => "TextBox[multiline]",
        _ => "TextBox",
    };

    private static ComponentStateDump BuildComponentState(ParameterViewModel param)
    {
        var name = param.Descriptor.Name;
        var kind = param.Descriptor.Kind;
        return param switch
        {
            BoolParameterViewModel b => new ComponentStateDump
            {
                IsChecked = b.IsChecked,
            },
            EnumParameterViewModel e => new ComponentStateDump
            {
                SelectedItem = e.SelectedValue,
            },
            NumericParameterViewModel n => new ComponentStateDump
            {
                NumericValue = n.NumericValue,
                Increment = n.Increment,
                FormatString = n.FormatString,
            },
            PathParameterViewModel p => new ComponentStateDump
            {
                Text = RedactText(name, kind, p.TextValue),
            },
            StringArrayParameterViewModel sa => new ComponentStateDump
            {
                Text = RedactText(name, kind, sa.TextValue),
                Placeholder = "Comma-separated values",
            },
            StringParameterViewModel s => new ComponentStateDump
            {
                Text = RedactText(name, kind, s.TextValue),
            },
            _ => new ComponentStateDump(),
        };
    }

    private static string RedactText(string propertyName, ParameterKind kind, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var redacted = GuiDumpRedaction.RedactValue(propertyName, kind, text);
        return redacted as string ?? redacted?.ToString() ?? string.Empty;
    }

    private static ParameterEditorDump BuildParameterEditorDump(CommandTabViewModel tab, ParameterViewModel param)
    {
        var d = param.Descriptor;
        return new ParameterEditorDump
        {
            Id = $"{tab.Name}.{d.Name}",
            Role = "parameter_editor",
            Component = new ComponentDump
            {
                Type = ResolveComponent(param),
                Label = param.LabelText,
                Tooltip = param.TooltipText,
                RequiredIndicator = d.IsRequired,
                Items = d.Kind == ParameterKind.Enum && d.EnumValues.Length > 0
                    ? [.. d.EnumValues]
                    : null,
                State = BuildComponentState(param),
            },
            Binding = new BindingDump
            {
                Property = new BoundPropertyDump
                {
                    Name = d.Name,
                    Type = d.PropertyType.Name,
                },
                Cli = new CliParameterDump
                {
                    Token = d.CliOption,
                    DisplayName = d.DisplayName,
                    Description = d.Description,
                    Kind = d.Kind.ToString(),
                    Required = d.IsRequired,
                    IsArgument = d.IsArgument,
                    IsParentOption = d.IsParentOption,
                    DefaultValue = NormalizeValue(GuiDumpRedaction.RedactValue(d.Name, d.Kind, d.DefaultValue)),
                    EnumValues = d.Kind == ParameterKind.Enum && d.EnumValues.Length > 0
                        ? [.. d.EnumValues]
                        : null,
                },
                Value = new BoundValueDump
                {
                    Current = NormalizeValue(GuiDumpRedaction.RedactValue(d.Name, d.Kind, param.GetValue())),
                    HasValue = param.HasValue,
                },
            },
            Validation = new ValidationDump
            {
                Required = d.IsRequired,
                HasValue = param.HasValue,
                IsMissingRequired = d.IsRequired && !param.HasValue,
            },
        };
    }

    private static object? NormalizeValue(object? raw) => raw switch
    {
        bool b => b,
        decimal dec => dec,
        string { Length: 0 } => null,
        string s => s,
        string[] { Length: 0 } => null,
        string[] arr => arr,
        Enum e => e.ToString(),
        null => null,
        _ => raw.ToString(),
    };

    internal sealed class WindowDump
    {
        public required string Title { get; init; }
        public required string SelectedTab { get; init; }
        public required ThemeDump Theme { get; init; }
        public required List<TabDump> Tabs { get; init; }
    }

    internal sealed class ThemeDump
    {
        public required string Current { get; init; }
        public required ThemeToggleButtonDump ToggleButton { get; init; }
    }

    internal sealed class ThemeToggleButtonDump
    {
        public required string Role { get; init; }
        public required string Icon { get; init; }
        public required string Tooltip { get; init; }
        public required string Command { get; init; }
    }

    internal sealed class TabDump
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string SelectedCommand { get; init; }
        public required List<string> AvailableCommands { get; init; }
        public required bool HasSubcommands { get; init; }
        public required bool IsExecuting { get; init; }
        public required bool HasPreset { get; init; }
        public required bool IsLogPanelExpanded { get; init; }
        public required CommandControlDump CancelButton { get; init; }
        public required LogPanelDump LogPanel { get; init; }
        public required List<ParameterEditorDump> Controls { get; init; }
        public required string Status { get; init; }
    }

    internal sealed class LogPanelDump
    {
        public required string Role { get; init; }
        public required bool IsExpanded { get; init; }
        public required string Header { get; init; }
        public required string WidthColumn { get; init; }
        public required LogPanelControlsDump Controls { get; init; }
        public required List<LogEntryDump> Entries { get; init; }
        public required int TotalEntries { get; init; }
        public required int OmittedEntries { get; init; }
    }

    internal sealed class LogPanelControlsDump
    {
        public required CommandControlDump CopyAll { get; init; }
        public required CommandControlDump Clear { get; init; }
        public required CommandControlDump Collapse { get; init; }
        public required CommandControlDump Expand { get; init; }
    }

    internal sealed class CommandControlDump
    {
        public required string Role { get; init; }
        public required string Icon { get; init; }
        public required string Tooltip { get; init; }
        public required string Command { get; init; }
        public required bool IsVisible { get; init; }
    }

    internal sealed class LogEntryDump
    {
        public required string Timestamp { get; init; }
        public required string DisplayTimestamp { get; init; }
        public required string Level { get; init; }
        public required string Message { get; init; }
    }

    internal sealed class ParameterEditorDump
    {
        public required string Id { get; init; }
        public required string Role { get; init; }
        public required ComponentDump Component { get; init; }
        public required BindingDump Binding { get; init; }
        public required ValidationDump Validation { get; init; }
    }

    internal sealed class ComponentDump
    {
        public required string Type { get; init; }
        public required string Label { get; init; }
        public required string Tooltip { get; init; }
        public required bool RequiredIndicator { get; init; }
        public List<string>? Items { get; init; }
        public required ComponentStateDump State { get; init; }
    }

    internal sealed class ComponentStateDump
    {
        public bool? IsChecked { get; init; }
        public string? SelectedItem { get; init; }
        public decimal? NumericValue { get; init; }
        public decimal? Increment { get; init; }
        public string? FormatString { get; init; }
        public string? Text { get; init; }
        public string? Placeholder { get; init; }
    }

    internal sealed class BindingDump
    {
        public required BoundPropertyDump Property { get; init; }
        public required CliParameterDump Cli { get; init; }
        public required BoundValueDump Value { get; init; }
    }

    internal sealed class BoundPropertyDump
    {
        public required string Name { get; init; }
        public required string Type { get; init; }
    }

    internal sealed class CliParameterDump
    {
        public required string Token { get; init; }
        public required string DisplayName { get; init; }
        public required string Description { get; init; }
        public required string Kind { get; init; }
        public required bool Required { get; init; }
        public required bool IsArgument { get; init; }
        public required bool IsParentOption { get; init; }
        public required object? DefaultValue { get; init; }
        public List<string>? EnumValues { get; init; }
    }

    internal sealed class BoundValueDump
    {
        public required object? Current { get; init; }
        public required bool HasValue { get; init; }
    }

    internal sealed class ValidationDump
    {
        public required bool Required { get; init; }
        public required bool HasValue { get; init; }
        public required bool IsMissingRequired { get; init; }
    }
}
