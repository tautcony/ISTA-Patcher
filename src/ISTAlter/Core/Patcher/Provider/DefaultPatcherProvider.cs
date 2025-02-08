// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2025 TautCony

namespace ISTAlter.Core.Patcher.Provider;

using ISTAlter.Utils;
using Serilog;

public class DefaultPatcherProvider : IPatcherProvider
{
    public List<PatchInfo> Patches { get; set; } = IPatcherProvider.GetPatches(typeof(EssentialPatchAttribute));

    private DefaultPatcherProvider()
    {
        Log.Debug("Loaded patches: {Patches}", string.Join(", ", this.Patches.Select(p => p.Method.Name)));
    }

    protected DefaultPatcherProvider(ISTAOptions.OptionalPatchOptions opts)
        : this()
    {
        switch (opts.Mode)
        {
            case ISTAOptions.ModeType.Standalone:
                this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(ValidationPatchAttribute)));
                this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(EnableOfflinePatchAttribute)));
                break;
            case ISTAOptions.ModeType.iLean:
            default:
                break;
        }

        if (opts.ENET)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(ENETPatchAttribute)));
        }

        if (opts.FinishedOperations)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(FinishedOPPatchAttribute)));
        }

        if (opts.SkipRequirementsCheck)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(RequirementsPatchAttribute)));
        }

        if (opts.DataNotSend)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(NotSendPatchAttribute)));
        }

        if (opts.UserAuthEnv)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(UserAuthPatchAttribute)));
        }

        if (opts.MarketLanguage != null)
        {
            this.Patches.Add(new PatchInfo(
                PatchUtils.PatchCommonServiceWrapper_GetMarketLanguage(opts.MarketLanguage),
                ((Delegate)PatchUtils.PatchCommonServiceWrapper_GetMarketLanguage).Method,
                0
            ));
        }

        if (opts.SkipSyncClientConfig)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(SyncClientConfigAttribute)));
        }

        if (opts.SkipFakeFSCReject)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(DisableFakeFSCRejectPatchAttribute)));
        }

        if (opts.AirClient)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(EnableAirClientPatchAttribute)));
        }

        if (opts.SkipBrandCompatibleCheck)
        {
            this.Patches.AddRange(IPatcherProvider.GetPatches(typeof(DisableBrandCompatibleCheckPatchAttribute)));
        }
    }

    public DefaultPatcherProvider(ISTAOptions.PatchOptions opts)
        : this((ISTAOptions.OptionalPatchOptions)opts)
    {
    }

    public string[] LoadFileList(string basePath)
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
            fileList = IPatcherProvider.DefaultLoadFileList(basePath);
        }

        return fileList;
    }
}
