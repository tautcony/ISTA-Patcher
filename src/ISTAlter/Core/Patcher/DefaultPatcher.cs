// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Core.Patcher;

using System.Reflection;
using dnlib.DotNet;
using ISTAlter.Utils;
using Serilog;

public class DefaultPatcher : IPatcher
{
    public List<(Func<ModuleDefMD, int> Delegater, MethodInfo Method)> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatchAttribute));

    private DefaultPatcher()
    {
        Log.Debug("Loaded patches: {Patches}", string.Join(", ", this.Patches.Select(p => p.Method.Name)));
    }

    protected DefaultPatcher(ISTAOptions.OptionalPatchOptions opts)
        : this()
    {
        switch (opts.Mode)
        {
            case ISTAOptions.ModeType.Standalone:
                this.Patches.AddRange(IPatcher.GetPatches(typeof(ValidationPatchAttribute)));
                this.Patches.AddRange(IPatcher.GetPatches(typeof(EnableOfflinePatchAttribute)));
                break;
            case ISTAOptions.ModeType.iLean:
            default:
                break;
        }

        if (opts.ENET)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ENETPatchAttribute)));
        }

        if (opts.FinishedOperations)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(FinishedOPPatchAttribute)));
        }

        if (opts.SkipRequirementsCheck)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(RequirementsPatchAttribute)));
        }

        if (opts.DataNotSend)
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
            this.Patches.AddRange(IPatcher.GetPatches(typeof(SyncClientConfigAttribute)));
        }

        if (opts.SkipFakeFSCReject)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(DisableFakeFSCRejectPatchAttribute)));
        }
    }

    public DefaultPatcher(ISTAOptions.PatchOptions opts)
        : this((ISTAOptions.OptionalPatchOptions)opts)
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
