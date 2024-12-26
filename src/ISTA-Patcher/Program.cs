// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher;

using ISTA_Patcher.Handlers;
using ISTA_Patcher.Tasks;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IStartupTask, LoggerTask>()
            .AddSingleton<IStartupTask, SentryTask>()
            .BuildServiceProvider();

        serviceProvider.GetServices<IStartupTask>()
            .ToList()
            .ForEach(startupTask => startupTask.Execute());

        var command = ProgramArgs.BuildCommandLine(
            PatchHandler.Execute,
            CerebrumancyHandler.Execute,
            DecryptHandler.Execute,
            iLeanHandler.Execute);

        var parseResult = command.Parse(args);
        var transaction = SentrySdk.StartTransaction("ISTA-Patcher", parseResult.CommandResult.Command.ToString());
        var result = parseResult.InvokeAsync();
        transaction.Finish();
        return result;
    }
}
