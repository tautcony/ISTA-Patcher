// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2025 TautCony

namespace ISTAlter.Core.Patcher.Provider;

using ISTAlter.Utils;

public class ToyotaPatcherProvider : IPatcherProvider
{
    public List<PatchInfo> Patches { get; set; } =
        IPatcherProvider.GetPatches(typeof(EssentialPatchAttribute), typeof(ValidationPatchAttribute), typeof(ToyotaPatchAttribute));

    public string?[] LoadFileList(string basePath)
    {
        return IPatcherProvider.DefaultLoadFileList(basePath);
    }
}
