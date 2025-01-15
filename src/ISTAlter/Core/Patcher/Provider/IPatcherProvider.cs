// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Core.Patcher.Provider;

using System.Reflection;
using System.Text.Json;
using dnlib.DotNet;
using ISTAlter.Utils;
using Serilog;

public interface IPatcherProvider
{
    public List<PatchInfo> Patches { get; set; }

    public string[] GeneratePatchList(string basePath);

    public static string?[] LoadFileList(string basePath)
    {
        var fileList = Directory.GetFiles(Constants.TesterGUIPath.Aggregate(basePath, Path.Join))
                                .Where(f => f.EndsWith(".exe", StringComparison.Ordinal) || f.EndsWith("dll", StringComparison.Ordinal))
                                .Select(Path.GetFileName).ToArray();
        return fileList;
    }

    public static PatchConfig? LoadConfigFile()
    {
        var cwd = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        try
        {
            using FileStream stream = new(Path.Join(cwd, "patch-config.json"), FileMode.Open, FileAccess.Read);
            var patchConfig = JsonSerializer.Deserialize(stream, PatchConfigSourceGenerationContext.Default.PatchConfig);
            return patchConfig;
        }
        catch (Exception ex) when (
            ex is FileNotFoundException or IOException or JsonException
        )
        {
            SentrySdk.CaptureException(ex);
            Log.Fatal(ex, "Failed to load config file: {Reason}", ex.Message);
        }

        return null;
    }

    public static List<PatchInfo> GetPatches(params Type[] attributes)
    {
        return typeof(PatchUtils)
            .GetMethods()
            .Where(m => Array.Exists(attributes, attribute => m.GetCustomAttributes(attribute, inherit: false).Length > 0))
            .Select(m => new PatchInfo((Func<ModuleDefMD, int>)Delegate.CreateDelegate(typeof(Func<ModuleDefMD, int>), m), m, 0))
            .ToList();
    }

    public static string[] ExtractLibrariesConfigFromAttribute(MethodInfo methodInfo)
    {
        return methodInfo
               .GetCustomAttributes(typeof(LibraryNameAttribute), inherit: false)
               .Select(attribute => ((LibraryNameAttribute)attribute).FileName)
               .SelectMany(i => i).ToArray();
    }
}
