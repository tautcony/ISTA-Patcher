// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTA_Patcher.Core.Patcher;

using System.Reflection;
using dnlib.DotNet;
using Serilog;

public class DefaultPatcher : IPatcher
{
    public List<(Func<ModuleDefMD, int> Delegater, MethodInfo Method)> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatchAttribute));

    private DefaultPatcher()
    {
        Log.Debug("Loaded patches: {Patches}", string.Join(", ", this.Patches.Select(p => p.Method.Name)));
    }

    protected DefaultPatcher(ProgramArgs.OptionalPatchOptions opts)
        : this()
    {
        if (!opts.SkipLicensePatch)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ValidationPatchAttribute)));
        }

        if (opts.EnableENET)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ENETPatchAttribute)));
        }

        if (opts.DisableRequirementsCheck)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(RequirementsPatchAttribute)));
        }

        if (opts.EnableNotSend)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(NotSendPatchAttribute)));
        }

        if (opts.UserAuthEnv)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(UserAuthPatchAttribute)));
        }

        if (opts.MarketLanguage != null)
        {
            this.Patches.Add((
                PatchUtils.PatchCommonServiceWrapper_GetMarketLanguage(opts.MarketLanguage),
                ((Delegate)PatchUtils.PatchCommonServiceWrapper_GetMarketLanguage).Method
            ));
        }

        if (opts.SkipSyncClientConfig)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(SkipSyncClientConfigAttribute)));
        }
    }

    public DefaultPatcher(ProgramArgs.PatchOptions opts)
        : this((ProgramArgs.OptionalPatchOptions)opts)
    {
    }

    private static string[] LoadFileList(string basePath)
    {
        var encryptedFileList = Constants.EncCnePath.Aggregate(basePath, Path.Join);

        // load file list from enc_cne_1.prg
        var fileList = Array.Empty<string>();
        if (File.Exists(encryptedFileList))
        {
            fileList = (IntegrityUtils.DecryptFile(encryptedFileList!) ?? [])
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
        var excludeList = patchConfig?.Exclude ?? [];
        var includeList = patchConfig?.Include ?? [];

        var patchList = includeList
                        .Union(fileList.Except(excludeList, StringComparer.Ordinal), StringComparer.Ordinal)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .ToArray();
        return patchList;
    }
}
