// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Core.Patcher;

using dnlib.DotNet;

public class ToyotaPatcher : IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatch), typeof(ValidationPatch), typeof(ToyotaPatch));

    public string[] GeneratePatchList(string basePath)
    {
        var fileList = IPatcher.LoadFileList(basePath);
        var patchConfig = IPatcher.LoadConfigFile();

        var excludeList = patchConfig?.ExcludeToyota ?? patchConfig?.Exclude ?? Array.Empty<string>();
        var includeList = patchConfig?.IncludeToyota ?? Array.Empty<string>();

        var patchList = includeList
                        .Union(fileList.Except(excludeList, StringComparer.Ordinal), StringComparer.Ordinal)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .ToArray();
        return patchList;
    }
}
