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

public class CommandTabViewModel : ObservableObject
{
    private bool _isExecuting;
    private string _statusText = "Ready";

    public CommandDescriptor Descriptor { get; }

    public string Name => Descriptor.Name;

    public string Description => Descriptor.Description;

    public ObservableCollection<ParameterViewModel> Parameters { get; }

    public ObservableCollection<string> OutputLines { get; } = [];

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

    public ICommand ExecuteCommandCommand { get; }

    public CommandTabViewModel(CommandDescriptor descriptor)
    {
        Descriptor = descriptor;
        Parameters = new ObservableCollection<ParameterViewModel>(
            descriptor.Parameters.Select(ParameterViewModel.Create));
        ExecuteCommandCommand = new AsyncRelayCommand(ExecuteCommandAsync, () => !IsExecuting);
    }

    private async Task ExecuteCommandAsync()
    {
        IsExecuting = true;
        StatusText = "Executing...";
        OutputLines.Clear();

        using var subscription = App.LogSink.Subscribe(msg =>
        {
            Dispatcher.UIThread.Post(() => OutputLines.Add(msg));
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
            OutputLines.Add($"Error: {message}");
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
