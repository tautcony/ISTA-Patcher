// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISTAvalon.Services;

public class MainWindowViewModel : ObservableObject
{
    private CommandTabViewModel? _selectedTab;

    public ObservableCollection<CommandTabViewModel> CommandTabs { get; }

    public CommandTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public MainWindowViewModel(IReadOnlyList<Models.CommandDescriptor>? descriptors = null)
    {
        descriptors ??= CommandDiscoveryService.DiscoverCommands();
        CommandTabs = new ObservableCollection<CommandTabViewModel>(
            descriptors.Select(d => new CommandTabViewModel(d)));
        SelectedTab = CommandTabs.FirstOrDefault();
    }
}
