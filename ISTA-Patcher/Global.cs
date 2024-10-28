// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher;

using Serilog.Core;

public class Global
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new();
}
