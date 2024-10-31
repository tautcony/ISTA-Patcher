// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using ISTA_Patcher.Core;
using ISTA_Patcher.Core.Patcher;
using Serilog;

public static class PatchHandler
{
    public static Task<int> Execute(ProgramArgs.PatchOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;
        var guiBasePath = Utils.Constants.TesterGUIPath.Aggregate(opts.TargetPath, Path.Join);
        var psdzBasePath = Utils.Constants.PSdZPath.Aggregate(opts.TargetPath, Path.Join);

        if (!Directory.Exists(guiBasePath) || !Directory.Exists(psdzBasePath))
        {
            Log.Fatal("Folder structure does not match under: {TargetPath}, please check options", opts.TargetPath);
            return Task.FromResult(-1);
        }

        IPatcher patcher = opts.PatchType switch
        {
            ProgramArgs.PatchType.B => new DefaultPatcher(opts),
            ProgramArgs.PatchType.T => new ToyotaPatcher(),
            _ => throw new NotSupportedException(),
        };

        Patch.PatchISTA(patcher, opts);
        return Task.FromResult(0);
    }
}
