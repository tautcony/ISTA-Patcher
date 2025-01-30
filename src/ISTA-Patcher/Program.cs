// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

using DotMake.CommandLine;
using ISTAPatcher.Commands;
using ISTAPatcher.Tasks;

TaskProvider.GatherTasks<IStartupTask>().Run();
var theme = new CliTheme
{
    DefaultColor = ConsoleColor.DarkGray,
    DefaultBgColor = (ConsoleColor)(-1),
    HeadingColor = ConsoleColor.Blue,
    FirstColumnColor = ConsoleColor.Cyan,
    SecondColumnColor = ConsoleColor.Green,
};
return await Cli.RunAsync<RootCommand>(args, new CliSettings { Theme = theme });
