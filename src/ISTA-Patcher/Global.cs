// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAPatcher;

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

public static class Global
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new();

    public static readonly IConfigurationRoot Config;

    static Global()
    {
        const string configFile = "appsettings.json";
        var basePath = AppContext.BaseDirectory;
        var configPath = Path.Combine(basePath, configFile);
        if (!File.Exists(configPath))
        {
            Log.Warning("Config file {ConfigFile} not found. Program behavior may differ from expectations.", configPath);
        }

        Config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(configFile, optional: true)
            .Build();
    }
}
