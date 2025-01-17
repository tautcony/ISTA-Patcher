// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAPatcher;

using ISTAPatcher.Handlers;
using ISTAPatcher.Tasks;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Global.ServicesProvider.GetServices<IStartupTask>()
            .ToList()
            .ForEach(startupTask => startupTask.Execute());

        var command = ProgramArgs.BuildCommandLine(
            PatchHandler.Execute,
            CerebrumancyHandler.Execute,
            CryptoHandler.Execute,
            iLeanHandler.Execute);

        var parseResult = command.Parse(args);
        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }
}
