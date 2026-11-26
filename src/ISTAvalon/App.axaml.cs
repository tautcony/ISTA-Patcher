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
    public static GuiObservationOptions ObservationOptions { get; set; } = new();

    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zz} [{Level:u3}]{PatchName:l} {Message:lj}{NewLine}{Exception}";

    private GuiObservationServer? _observationServer;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("PatchName", string.Empty)
            .MinimumLevel.ControlledBy(Global.LevelSwitch)
            .WriteTo.File("istalon.log", rollingInterval: RollingInterval.Day, outputTemplate: OutputTemplate)
            .WriteTo.Sink(LogSink)
            .CreateLogger();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            if (ObservationOptions.Enabled)
            {
                try
                {
                    _observationServer = new GuiObservationServer(viewModel, ObservationOptions);
                    _observationServer.Start();
                    desktop.Exit += (_, _) => _observationServer?.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        ex,
                        "GUI observation server failed to start on {Url}; continuing without observation",
                        ObservationOptions.Prefix);
                    _observationServer?.Dispose();
                    _observationServer = null;
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
