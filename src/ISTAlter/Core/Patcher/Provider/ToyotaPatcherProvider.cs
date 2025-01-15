// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Core.Patcher.Provider;

using ISTAlter.Utils;

public class ToyotaPatcherProvider : IPatcherProvider
{
    public List<PatchInfo> Patches { get; set; } =
        IPatcherProvider.GetPatches(typeof(EssentialPatchAttribute), typeof(ValidationPatchAttribute), typeof(ToyotaPatchAttribute));

    public string[] GeneratePatchList(string basePath)
    {
        var fileList = IPatcherProvider.LoadFileList(basePath);
        var patchConfig = IPatcherProvider.LoadConfigFile();

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
