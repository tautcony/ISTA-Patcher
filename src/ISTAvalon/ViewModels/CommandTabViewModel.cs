// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using Avalonia.Input.Platform;
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
    private CommandDescriptor? _selectedCommand;
    private CancellationTokenSource? _executionCancellationTokenSource;

    public CommandDescriptor RootDescriptor { get; }

    public string Name => RootDescriptor.Name;

    public ObservableCollection<CommandDescriptor> AvailableCommands { get; }

    public bool HasSubcommands => AvailableCommands.Count > 1;

    public CommandDescriptor? SelectedCommand
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

    public string Description => SelectedCommand?.Description ?? string.Empty;

    public ObservableCollection<ParameterViewModel> Parameters { get; private set; } = [];

    public ObservableCollection<LogEntry> OutputLines { get; } = [];

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                ((AsyncRelayCommand)ExecuteCommandCommand).NotifyCanExecuteChanged();
                ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
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

    public ICommand CancelCommand { get; }

    public ICommand ClearOutputCommand { get; }

    public ICommand CopyAllCommand { get; }

    public ICommand ToggleLogPanelCommand { get; }

    public ICommand ResetToPresetCommand { get; }

    public bool HasPreset { get; }

    private readonly IReadOnlyDictionary<string, string>? _preset;

    public CommandTabViewModel(CommandDescriptor descriptor, IReadOnlyDictionary<string, string>? preset = null)
    {
        _preset = preset;
        RootDescriptor = descriptor;
        AvailableCommands = [.. FlattenCommands(descriptor)];
        SelectedCommand = AvailableCommands.FirstOrDefault();

        HasPreset = preset is { Count: > 0 };
        ExecuteCommandCommand = new AsyncRelayCommand(
            ExecuteCommandAsync,
            () => !IsExecuting && !GuiCommandExecutionGate.IsBusy);
        CancelCommand = new RelayCommand(CancelExecution, () => IsExecuting);
        ClearOutputCommand = new RelayCommand(ClearOutput);
        CopyAllCommand = new AsyncRelayCommand(CopyAllAsync);
        ToggleLogPanelCommand = new RelayCommand(() => IsLogPanelExpanded = !IsLogPanelExpanded);
        ResetToPresetCommand = new RelayCommand(ResetToPreset, () => HasPreset);

        GuiCommandExecutionGate.StateChanged += OnGlobalExecutionStateChanged;
    }

    private void OnGlobalExecutionStateChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Notify();
        }
        else
        {
            Dispatcher.UIThread.Post(Notify);
        }

        return;

        void Notify()
        {
            ((AsyncRelayCommand)ExecuteCommandCommand).NotifyCanExecuteChanged();
        }
    }

    private static List<CommandDescriptor> FlattenCommands(CommandDescriptor root)
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

    private ObservableCollection<ParameterViewModel> ResolveParametersFor(CommandDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return [];
        }

        if (_parameterStateByCommand.TryGetValue(descriptor.CommandType, out var parameters))
        {
            return parameters;
        }

        parameters =
        [
            .. descriptor.Parameters
                .OrderByDescending(p => p.IsRequired)
                .Select(ParameterViewModel.Create)
        ];

        ApplyPresetToParameters(parameters);
        _parameterStateByCommand[descriptor.CommandType] = parameters;
        return parameters;
    }

    private void ApplyPresetToParameters(IEnumerable<ParameterViewModel> parameters)
    {
        if (_preset is null)
        {
            return;
        }

        foreach (var param in parameters)
        {
            if (_preset.TryGetValue(param.Descriptor.Name, out var presetValue))
            {
                param.ApplyValue(presetValue);
            }
        }
    }

    private void ResetToPreset()
    {
        foreach (var paramCollection in _parameterStateByCommand.Values)
        {
            ApplyPresetToParameters(paramCollection);
        }
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
        var selectedCommand = SelectedCommand;
        if (selectedCommand is null)
        {
            StatusText = "⚠ No command selected.";
            return;
        }

        var missing = Parameters
            .Where(p => p.Descriptor.IsRequired && !p.HasValue)
            .Select(p => p.Descriptor.DisplayName)
            .ToList();

        if (missing.Count > 0)
        {
            StatusText = $"⚠ Required: {string.Join(", ", missing)}";
            return;
        }

        if (!GuiCommandExecutionGate.TryEnter())
        {
            StatusText = "⚠ Another command is already running.";
            return;
        }

        IsExecuting = true;
        StatusText = "Executing...";
        OutputLines.Clear();
        _executionCancellationTokenSource = new CancellationTokenSource();

        using var subscription = App.LogSink.Subscribe(entry =>
        {
            Dispatcher.UIThread.Post(() => OutputLines.Add(entry));
        });

        try
        {
            var cancellationToken = _executionCancellationTokenSource.Token;
            var result = await Task.Run(() =>
                CommandExecutionService.ExecuteAsync(selectedCommand, Parameters, cancellationToken), cancellationToken);

            StatusText = result == 0
                ? "✓ Command completed successfully."
                : $"⚠ Command finished with exit code {result}.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Command cancelled.";
            OutputLines.Add(new LogEntry(DateTimeOffset.Now, LogEventLevel.Warning, "Command cancelled."));
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            StatusText = $"✗ Command failed: {message}";
            OutputLines.Add(new LogEntry(DateTimeOffset.Now, LogEventLevel.Error, $"Error: {message}"));
        }
        finally
        {
            _executionCancellationTokenSource?.Dispose();
            _executionCancellationTokenSource = null;
            IsExecuting = false;
            GuiCommandExecutionGate.Exit();
        }
    }

    private void CancelExecution()
    {
        if (_executionCancellationTokenSource is null || _executionCancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        StatusText = "Cancelling...";
        _executionCancellationTokenSource.Cancel();
    }
}
