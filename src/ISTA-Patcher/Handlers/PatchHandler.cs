// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Core.Patcher;
using ISTAlter.Utils;
using Serilog;

public static class PatchHandler
{
    public static Task<int> Execute(ISTAOptions.PatchOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;
        var guiBasePath = Constants.TesterGUIPath.Aggregate(opts.TargetPath, Path.Join);
        var psdzBasePath = Constants.PSdZPath.Aggregate(opts.TargetPath, Path.Join);

        if (!Directory.Exists(guiBasePath) || !Directory.Exists(psdzBasePath))
        {
            Log.Fatal("Folder structure does not match under: {TargetPath}, please check options", opts.TargetPath);
            return Task.FromResult(-1);
        }

        IPatcher patcher = opts.PatchType switch
        {
            ISTAOptions.PatchType.B => new DefaultPatcher(opts),
            ISTAOptions.PatchType.T => new ToyotaPatcher(),
            _ => throw new NotSupportedException(),
        };

        Patch.PatchISTA(patcher, opts);
        return Task.FromResult(0);
    }
}
