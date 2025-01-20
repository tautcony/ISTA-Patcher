// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

namespace ISTAPatcher;

using DotMake.CommandLine;
using ISTAPatcher.Commands;
using ISTAPatcher.Tasks;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Global.ServicesProvider.GetServices<IStartupTask>().Run();
        var theme = new CliTheme
        {
            DefaultColor = ConsoleColor.White,
            DefaultBgColor = null,
            HeadingColor = ConsoleColor.Blue,
            FirstColumnColor = ConsoleColor.Cyan,
            SecondColumnColor = ConsoleColor.Green,
        };
        return await Cli.RunAsync<RootCommand>(args, new CliSettings { Theme = theme });
    }
}
