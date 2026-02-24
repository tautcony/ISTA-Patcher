// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ISTAvalon.Services;
using ISTAvalon.ViewModels;
using ISTAvalon.Views;
using ISTAPatcher;
using Serilog;

public class App : Application
{
    public static DelegateLogSink LogSink { get; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(Global.LevelSwitch)
            .WriteTo.File("istalon.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Sink(LogSink)
            .CreateLogger();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
