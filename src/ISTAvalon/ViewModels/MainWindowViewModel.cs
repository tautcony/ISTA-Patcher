// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISTAvalon.Models;
using ISTAvalon.Services;

public class MainWindowViewModel : ObservableObject
{
    private CommandTabViewModel? _selectedTab;
    private AppTheme _currentTheme;
    private readonly GuiSettings _settings;
    private AppMetadata AppMetadata { get; }

    public ObservableCollection<CommandTabViewModel> CommandTabs { get; }

    public string WindowTitle => AppMetadata.WindowTitle;

    public CommandTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    /// <summary>
    /// The active theme mode. Cycles Default → Light → Dark → Default on each
    /// call to <see cref="ToggleThemeCommand"/>.
    /// </summary>
    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (SetProperty(ref _currentTheme, value))
            {
                OnPropertyChanged(nameof(ThemeIcon));
                OnPropertyChanged(nameof(ThemeTooltip));
                ApplyTheme();
                _settings.Theme = value.ToString();
                GuiSettingsService.Save(_settings);
            }
        }
    }

    /// <summary>
    /// Icon representing the <em>next</em> state the toggle will move to,
    /// consistent with the pre-existing two-state convention.
    /// Default → sun (will go Light), Light → moon (will go Dark), Dark → circle-half (will go System).
    /// </summary>
    public string ThemeIcon => _currentTheme switch
    {
        AppTheme.Default => "fa-solid fa-sun",
        AppTheme.Light   => "fa-solid fa-moon",
        AppTheme.Dark    => "fa-solid fa-circle-half-stroke",
        _                => "fa-solid fa-sun",
    };

    /// <summary>Tooltip describing what the next toggle click will do.</summary>
    public string ThemeTooltip => _currentTheme switch
    {
        AppTheme.Default => "Switch to Light Theme",
        AppTheme.Light   => "Switch to Dark Theme",
        AppTheme.Dark    => "Follow System Theme",
        _                => "Switch to Light Theme",
    };

    public ICommand ToggleThemeCommand { get; }

    public MainWindowViewModel(IReadOnlyList<Models.CommandDescriptor>? descriptors = null)
    {
        AppMetadata = AppMetadataProvider.Get();
        _settings = GuiSettingsService.Load();
        descriptors ??= CommandDiscoveryService.DiscoverCommands();
        CommandTabs =
        [
            .. descriptors.Select(d => new CommandTabViewModel(d, _settings.GetPresetFor(d.Name)))
        ];
        SelectedTab = CommandTabs.FirstOrDefault();

        ToggleThemeCommand = new RelayCommand(CycleTheme);

        _currentTheme = Enum.TryParse<AppTheme>(_settings.Theme, ignoreCase: true, out var loaded)
            ? loaded
            : AppTheme.Default;
        ApplyTheme();
    }

    private void CycleTheme()
    {
        CurrentTheme = _currentTheme switch
        {
            AppTheme.Default => AppTheme.Light,
            AppTheme.Light   => AppTheme.Dark,
            AppTheme.Dark    => AppTheme.Default,
            _                => AppTheme.Light,
        };
    }

    private void ApplyTheme()
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = _currentTheme switch
            {
                AppTheme.Dark  => ThemeVariant.Dark,
                AppTheme.Light => ThemeVariant.Light,
                _              => ThemeVariant.Default,
            };
        }
    }
}
