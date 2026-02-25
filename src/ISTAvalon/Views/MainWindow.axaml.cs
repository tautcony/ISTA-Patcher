// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Views;

using Serilog;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ISTAvalon.Models;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnCopyLineClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is MenuItem { Tag: LogEntry entry } && Clipboard is { } clipboard)
            {
                var text = $"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}";
                await clipboard.SetTextAsync(text);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while copying log entry to clipboard");
        }
    }

}
