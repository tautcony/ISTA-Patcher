// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISTAvalon.Models;
using ISTAvalon.Services;
using Serilog.Events;

public class CommandTabViewModel : ObservableObject
{
    private readonly Dictionary<Type, ObservableCollection<ParameterViewModel>> _parameterStateByCommand = [];

    private bool _isExecuting;
    private string _statusText = "Ready";
    private bool _isLogPanelExpanded = true;
    private CommandDescriptor _selectedCommand;

    public CommandDescriptor RootDescriptor { get; }

    public string Name => RootDescriptor.Name;

    public ObservableCollection<CommandDescriptor> AvailableCommands { get; }

    public bool HasSubcommands => AvailableCommands.Count > 1;

    public CommandDescriptor SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            if (SetProperty(ref _selectedCommand, value))
            {
                Parameters = ResolveParametersFor(value);
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(Parameters));
            }
        }
    }

    public string Description => SelectedCommand.Description;

    public ObservableCollection<ParameterViewModel> Parameters { get; private set; }

    public ObservableCollection<LogEntry> OutputLines { get; } = [];

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                ((AsyncRelayCommand)ExecuteCommandCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsLogPanelExpanded
    {
        get => _isLogPanelExpanded;
        set => SetProperty(ref _isLogPanelExpanded, value);
    }

    public ICommand ExecuteCommandCommand { get; }

    public ICommand ClearOutputCommand { get; }

    public ICommand CopyAllCommand { get; }

    public ICommand ToggleLogPanelCommand { get; }

    public CommandTabViewModel(CommandDescriptor descriptor)
    {
        RootDescriptor = descriptor;
        AvailableCommands = new ObservableCollection<CommandDescriptor>(FlattenCommands(descriptor));
        SelectedCommand = AvailableCommands[0];
        Parameters = ResolveParametersFor(SelectedCommand);

        ExecuteCommandCommand = new AsyncRelayCommand(ExecuteCommandAsync, () => !IsExecuting);
        ClearOutputCommand = new RelayCommand(ClearOutput);
        CopyAllCommand = new AsyncRelayCommand(CopyAllAsync);
        ToggleLogPanelCommand = new RelayCommand(() => IsLogPanelExpanded = !IsLogPanelExpanded);
    }

    private static IReadOnlyList<CommandDescriptor> FlattenCommands(CommandDescriptor root)
    {
        var result = new List<CommandDescriptor> { root };
        AddSubcommands(root, result);
        return result;

        static void AddSubcommands(CommandDescriptor descriptor, ICollection<CommandDescriptor> bag)
        {
            foreach (var child in descriptor.Subcommands)
            {
                bag.Add(child);
                AddSubcommands(child, bag);
            }
        }
    }

    private ObservableCollection<ParameterViewModel> ResolveParametersFor(CommandDescriptor descriptor)
    {
        if (_parameterStateByCommand.TryGetValue(descriptor.CommandType, out var parameters))
        {
            return parameters;
        }

        parameters = new ObservableCollection<ParameterViewModel>(
            descriptor.Parameters
                .OrderByDescending(p => p.IsRequired)
                .Select(ParameterViewModel.Create));

        _parameterStateByCommand[descriptor.CommandType] = parameters;
        return parameters;
    }

    private void ClearOutput()
    {
        OutputLines.Clear();
        StatusText = "Ready";
    }

    private async Task CopyAllAsync()
    {
        var text = string.Join(Environment.NewLine,
            OutputLines.Select(e => $"{e.Timestamp:HH:mm:ss.fff} [{e.Level}] {e.Message}"));
        var clipboard = Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.Clipboard
            : null;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    private async Task ExecuteCommandAsync()
    {
        var missing = Parameters
            .Where(p => p.Descriptor.IsRequired && !p.HasValue)
            .Select(p => p.Descriptor.DisplayName)
            .ToList();

        if (missing.Count > 0)
        {
            StatusText = $"⚠ Required: {string.Join(", ", missing)}";
            return;
        }

        IsExecuting = true;
        StatusText = "Executing...";
        OutputLines.Clear();

        using var subscription = App.LogSink.Subscribe(entry =>
        {
            Dispatcher.UIThread.Post(() => OutputLines.Add(entry));
        });

        try
        {
            var result = await Task.Run(() =>
                CommandExecutionService.ExecuteAsync(SelectedCommand, Parameters));

            StatusText = result == 0
                ? "✓ Command completed successfully."
                : $"⚠ Command finished with exit code {result}.";
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            StatusText = $"✗ Command failed: {message}";
            OutputLines.Add(new LogEntry(DateTimeOffset.Now, LogEventLevel.Error, $"Error: {message}"));
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
