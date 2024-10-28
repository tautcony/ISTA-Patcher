// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTA_Patcher.Core.Patcher;

using System.Reflection;
using dnlib.DotNet;
using ISTA_Patcher.Utils;

public class ToyotaPatcher : IPatcher
{
    public List<(Func<ModuleDefMD, int> Delegater, MethodInfo Method)> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatchAttribute), typeof(ValidationPatchAttribute), typeof(ToyotaPatchAttribute));

    public string[] GeneratePatchList(string basePath)
    {
        var fileList = IPatcher.LoadFileList(basePath);
        var patchConfig = IPatcher.LoadConfigFile();

        var excludeList = patchConfig?.ExcludeToyota ?? patchConfig?.Exclude ?? [];
        var includeList = patchConfig?.IncludeToyota ?? [];

        var patchList = includeList
                        .Union(fileList.Except(excludeList, StringComparer.Ordinal), StringComparer.Ordinal)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .ToArray();
        return patchList;
    }
}
