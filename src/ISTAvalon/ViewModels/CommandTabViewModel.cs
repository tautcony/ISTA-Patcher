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
    private bool _isExecuting;
    private string _statusText = "Ready";
    private bool _isLogPanelExpanded = true;

    public CommandDescriptor Descriptor { get; }

    public string Name => Descriptor.Name;

    public string Description => Descriptor.Description;

    public ObservableCollection<ParameterViewModel> Parameters { get; }

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
        Descriptor = descriptor;
        Parameters = new ObservableCollection<ParameterViewModel>(
            descriptor.Parameters.Select(ParameterViewModel.Create));
        ExecuteCommandCommand = new AsyncRelayCommand(ExecuteCommandAsync, () => !IsExecuting);
        ClearOutputCommand = new RelayCommand(ClearOutput);
        CopyAllCommand = new AsyncRelayCommand(CopyAllAsync);
        ToggleLogPanelCommand = new RelayCommand(() => IsLogPanelExpanded = !IsLogPanelExpanded);
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
                CommandExecutionService.ExecuteAsync(Descriptor, Parameters));

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
