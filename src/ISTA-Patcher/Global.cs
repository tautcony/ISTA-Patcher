// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher;

using Microsoft.Extensions.Configuration;
using Serilog.Core;

public static class Global
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new();

    public static readonly IConfigurationRoot Config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).Build();
}
