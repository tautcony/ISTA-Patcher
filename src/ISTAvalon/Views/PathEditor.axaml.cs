// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using Serilog;

namespace ISTAvalon.Views;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ISTAvalon.ViewModels;

public partial class PathEditor : UserControl
{
    public PathEditor()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not PathParameterViewModel vm)
        {
            return;
        }

        foreach (var transferItem in e.DataTransfer.GetItems(DataFormat.File))
        {
            if (transferItem.TryGetRaw(DataFormat.File) is not IStorageItem storageItem)
            {
                continue;
            }

            var path = storageItem.TryGetLocalPath();
            if (path != null && Directory.Exists(path))
            {
                vm.TextValue = path;
                return;
            }
        }
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not PathParameterViewModel vm)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is not { } storageProvider)
            {
                return;
            }

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select Folder",
            });

            if (folders.Count > 0)
            {
                var path = folders[0].TryGetLocalPath();
                if (path != null)
                {
                    vm.TextValue = path;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while browsing for folder.");
        }
    }
}
