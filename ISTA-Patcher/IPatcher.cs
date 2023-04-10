// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher;

using System.Text.Json;
using Serilog;
using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

public interface IPatcher
{
    public Func<AssemblyDefinition, bool>[] Patches { get; set; }

    public string[] GeneratePatchList(string basePath);

    public static string?[] LoadFileList(string basePath)
    {
        var fileList = Directory.GetFiles(Path.Join(basePath, "TesterGUI", "bin", "Release"))
                                .Where(f => f.EndsWith(".exe") || f.EndsWith("dll"))
                                .Select(Path.GetFileName).ToArray();
        return fileList;
    }

    public static Dictionary<string, string[]>? LoadConfigFile()
    {
        var cwd = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        try
        {
            using FileStream stream = new(Path.Join(cwd, "patchConfig.json"), FileMode.Open, FileAccess.Read);
            var patchConfig = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream);
            return patchConfig;
        }
        catch (Exception ex) when (
            ex is FileNotFoundException or IOException or JsonException
        )
        {
            Log.Fatal(ex, "Failed to load config file: {Reason}", ex.Message);
        }

        return null;
    }
}

public class BMWPatcher : IPatcher
{
    public Func<AssemblyDefinition, bool>[] Patches { get; set; } =
    {
        PatchUtils.PatchIntegrityManager,
        PatchUtils.PatchLicenseStatusChecker,
        PatchUtils.PatchCheckSignature,
        PatchUtils.PatchLicenseManager,
        PatchUtils.PatchAOSLicenseManager,
        PatchUtils.PatchIstaIcsServiceClient,
        PatchUtils.PatchCommonServiceWrapper,
        PatchUtils.PatchSecureAccessHelper,
        PatchUtils.PatchLicenseWizardHelper,
        PatchUtils.PatchVerifyAssemblyHelper,
        PatchUtils.PatchFscValidationClient,
        PatchUtils.PatchMainWindowViewModel,
        PatchUtils.PatchActivationCertificateHelper,
        PatchUtils.PatchCertificateHelper,
        PatchUtils.PatchConfigurationService,
        PatchUtils.PatchInteractionAdministrationModel,
        PatchUtils.PatchCompileTime,
    };

    public static string?[] LoadFileList(string basePath)
    {
        var encryptedFileList = Path.Join(basePath, "Ecu", "enc_cne_1.prg");

        // load file list from enc_cne_1.prg
        var fileList = (IntegrityManager.DecryptFile(encryptedFileList!) ?? new List<HashFileInfo>())
                       .Select(f => f.FileName).ToArray();

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
        var excludeList = patchConfig?.GetValueOrDefault("exclude") ?? Array.Empty<string>();
        var includeList = patchConfig?.GetValueOrDefault("include") ?? Array.Empty<string>();

        var patchList = includeList
                        .Union(fileList.Where(f => !excludeList.Contains(f)))
                        .Distinct()
                        .OrderBy(i => i).ToArray();
        return patchList;
    }
}

public class ToyotaPatcher : IPatcher
{
    public Func<AssemblyDefinition, bool>[] Patches { get; set; } =
    {
        PatchUtils.PatchIntegrityManager,
        PatchUtils.PatchLicenseStatusChecker,
        PatchUtils.PatchCheckSignature,
        PatchUtils.PatchLicenseManager,
        PatchUtils.PatchAOSLicenseManager,
        PatchUtils.PatchIstaIcsServiceClient,
        PatchUtils.PatchCommonServiceWrapper,
        PatchUtils.PatchSecureAccessHelper,
        PatchUtils.PatchLicenseWizardHelper,
        PatchUtils.PatchVerifyAssemblyHelper,
        PatchUtils.PatchFscValidationClient,
        PatchUtils.PatchMainWindowViewModel,
        PatchUtils.PatchActivationCertificateHelper,
        PatchUtils.PatchCertificateHelper,
        PatchUtils.PatchConfigurationService,
        PatchUtils.PatchCommonFuncForIsta,
        PatchUtils.PatchPackageValidityService,
        PatchUtils.PatchToyotaWorker,
    };

    public string[] GeneratePatchList(string basePath)
    {
        var fileList = IPatcher.LoadFileList(basePath);
        var patchConfig = IPatcher.LoadConfigFile();
        var excludeList = patchConfig?.GetValueOrDefault("exclude") ?? Array.Empty<string>();
        var includeList = patchConfig?.GetValueOrDefault("include.toyota") ?? Array.Empty<string>();

        var patchList = includeList
                        .Union(fileList.Where(f => !excludeList.Contains(f)))
                        .Distinct()
                        .OrderBy(i => i).ToArray();
        return patchList;
    }
}

public class BMWLicensePatcher : BMWPatcher
{
    public BMWLicensePatcher(string modulus, string exponent)
    {
        this.Patches = new[]
        {
            PatchUtils.PatchIntegrityManager,
            PatchUtils.PatchVerifyAssemblyHelper,
            PatchUtils.PatchConfigurationService,
            PatchUtils.PatchInteractionAdministrationModel,
            PatchUtils.GeneratePatchGetRSAPKCS1SignatureDeformatter(modulus, exponent),
            PatchUtils.PatchCompileTime,
        };
    }
}
