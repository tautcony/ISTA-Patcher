// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable InconsistentNaming
namespace ISTA_Patcher;

using System.Text.Json;
using dnlib.DotNet;
using Serilog;
using LicenseOptions = ProgramArgs.LicenseOptions;
using OptionalPatchOptions = ProgramArgs.OptionalPatchOptions;
using PatchOptions = ProgramArgs.PatchOptions;

public interface IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; }

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

    public static List<Func<ModuleDefMD, int>> GetPatches(params Type[] attributes)
    {
        return typeof(PatchUtils)
            .GetMethods()
            .Where(m => attributes.Any(attribute => m.GetCustomAttributes(attribute, false).Length > 0))
            .Select(m => (Func<ModuleDefMD, int>)Delegate.CreateDelegate(typeof(Func<ModuleDefMD, int>), m))
            .ToList();
    }
}

public class BMWPatcher : IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatch));

    private BMWPatcher()
    {
        Log.Debug("Loaded patches: {Patches}", string.Join(", ", this.Patches.Select(p => p.Method.Name)));
    }

    public BMWPatcher(OptionalPatchOptions opts)
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

    public BMWPatcher(PatchOptions opts)
        : this((OptionalPatchOptions)opts)
    {
        if (!opts.SkipLicensePatch)
        {
            this.Patches.AddRange(IPatcher.GetPatches(typeof(ValidationPatch)));
        }
    }

    public static string?[] LoadFileList(string basePath)
    {
        var encryptedFileList = Path.Join(basePath, "Ecu", "enc_cne_1.prg");

        // load file list from enc_cne_1.prg
        var fileList = Array.Empty<string>();
        if (File.Exists(encryptedFileList))
        {
            fileList = (IntegrityManager.DecryptFile(encryptedFileList!) ?? new List<HashFileInfo>())
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
        var excludeList = patchConfig?.GetValueOrDefault("exclude") ?? Array.Empty<string>();
        var includeList = patchConfig?.GetValueOrDefault("include") ?? Array.Empty<string>();

        var patchList = includeList
                        .Union(fileList.Except(excludeList))
                        .Distinct()
                        .Order()
                        .ToArray();
        return patchList;
    }
}

public class ToyotaPatcher : IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; } =
        IPatcher.GetPatches(typeof(EssentialPatch), typeof(ValidationPatch), typeof(ToyotaPatch));

    public string[] GeneratePatchList(string basePath)
    {
        var fileList = IPatcher.LoadFileList(basePath);
        var patchConfig = IPatcher.LoadConfigFile();
        var excludeList = patchConfig?.GetValueOrDefault("exclude") ?? Array.Empty<string>();
        var includeList = patchConfig?.GetValueOrDefault("include.toyota") ?? Array.Empty<string>();

        var patchList = includeList
                        .Union(fileList.Except(excludeList))
                        .Distinct()
                        .Order()
                        .ToArray();
        return patchList;
    }
}

public class BMWLicensePatcher : BMWPatcher
{
    public BMWLicensePatcher(string modulus, string exponent, LicenseOptions opts)
        : base(opts)
    {
        this.Patches.Add(
            PatchUtils.PatchGetRSAPKCS1SignatureDeformatter(modulus, exponent)
        );
    }
}
