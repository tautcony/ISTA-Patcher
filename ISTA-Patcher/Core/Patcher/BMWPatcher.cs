// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

// ReSharper disable InconsistentNaming, RedundantNameQualifier
namespace ISTA_Patcher.Core.Patcher;

using dnlib.DotNet;
using Serilog;

public class BMWPatcher : IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatch));

    private BMWPatcher()
    {
        Log.Debug("Loaded patches: {Patches}", string.Join(", ", this.Patches.Select(p => p.Method.Name)));
    }

    protected BMWPatcher(ProgramArgs.OptionalPatchOptions opts)
        : this()
    {
        if (opts.EnableENET)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ENETPatch)));
        }

        if (opts.DisableRequirementsCheck)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(RequirementsPatch)));
        }

        if (opts.EnableNotSend)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(NotSendPatch)));
        }
    }

    public BMWPatcher(ProgramArgs.PatchOptions opts)
        : this((ProgramArgs.OptionalPatchOptions)opts)
    {
        if (!opts.SkipLicensePatch)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ValidationPatch)));
        }
    }

    private static string?[] LoadFileList(string basePath)
    {
        var encryptedFileList = Path.Join(basePath, "Ecu", "enc_cne_1.prg");

        // load file list from enc_cne_1.prg
        var fileList = Array.Empty<string>();
        if (File.Exists(encryptedFileList))
        {
            fileList = (IntegrityUtils.DecryptFile(encryptedFileList!) ?? new List<HashFileInfo>())
                       .Select(f => f.FileName).ToArray();
        }
        else
        {
            Log.Warning("File {File} not found, fallback to load from directory", encryptedFileList);
        }

        // or from directory ./TesterGUI/bin/Release
        if (fileList.Length == 0)
        {
            fileList = IPatcher.LoadFileList(basePath);
        }

        return fileList;
    }

    public string[] GeneratePatchList(string basePath)
    {
        var fileList = LoadFileList(basePath);
        var patchConfig = IPatcher.LoadConfigFile();
        var excludeList = patchConfig?.Exclude ?? Array.Empty<string>();
        var includeList = patchConfig?.Include ?? Array.Empty<string>();

        var patchList = includeList
                        .Union(fileList.Except(excludeList))
                        .Distinct()
                        .Order()
                        .ToArray();
        return patchList;
    }
}
