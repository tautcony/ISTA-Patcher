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
        Global.ServicesProvider.GetServices<IStartupTask>()
            .ToList()
            .ForEach(startupTask => startupTask.Execute());

        var command = ProgramArgs.BuildCommandLine(
            PatchHandler.Execute,
            CerebrumancyHandler.Execute,
            DecryptHandler.Execute,
            iLeanHandler.Execute);

        var parseResult = command.Parse(args);
        Global.Transaction = SentrySdk.StartTransaction("ISTA-Patcher", parseResult.CommandResult.Command.ToString());
        Global.Transaction.SetExtra("args", args);
        try
        {
            return parseResult.InvokeAsync();
        }
        finally
        {
            Global.Transaction.Finish();
        }
    }
}
