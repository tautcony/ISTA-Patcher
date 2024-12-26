// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Tasks;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

public class LoggerTask : IStartupTask
{
    public void Execute()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.ControlledBy(Global.LevelSwitch)
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .WriteTo.File("ista-patcher.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Sentry(LogEventLevel.Error, LogEventLevel.Debug)
            .CreateLogger();
    }
}
