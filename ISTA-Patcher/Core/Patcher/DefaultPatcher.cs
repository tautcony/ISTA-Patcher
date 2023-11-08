// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Core.Patcher;

using dnlib.DotNet;
using Serilog;

public class DefaultPatcher : IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatch));

    private DefaultPatcher()
    {
        Log.Debug("Loaded patches: {Patches}", string.Join(", ", this.Patches.Select(p => p.Method.Name)));
    }

    protected DefaultPatcher(ProgramArgs.OptionalPatchOptions opts)
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

    public DefaultPatcher(ProgramArgs.PatchOptions opts)
        : this((ProgramArgs.OptionalPatchOptions)opts)
    {
        if (!opts.SkipLicensePatch)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ValidationPatch)));
        }
    }

    private static IEnumerable<string> LoadFileList(string basePath)
    {
        var encryptedFileList = Constants.EncCnePath.Aggregate(basePath, Path.Join);

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
