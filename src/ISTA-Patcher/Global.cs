// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher;

using ISTA_Patcher.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

public static class Global
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new();

    public static ITransactionTracer? Transaction { get; set; }

    public static ServiceProvider ServicesProvider => ServiceProviderFactory.Instance;
}
