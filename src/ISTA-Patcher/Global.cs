// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAPatcher;

using ISTAPatcher.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

public static class Global
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new();

    public static ServiceProvider ServicesProvider => ServiceProviderFactory.Instance;
}
