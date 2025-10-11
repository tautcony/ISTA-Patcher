// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Tasks;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

public class LoggerTask : IStartupTask
{
    public const string LOGFILE = "ista-patcher.log";

    public void Execute(object?[]? parameters)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.ControlledBy(Global.LevelSwitch)
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .WriteTo.File(LOGFILE, rollingInterval: RollingInterval.Day)
            .WriteTo.Sentry(LogEventLevel.Error, LogEventLevel.Debug)
            .CreateLogger();
    }
}
