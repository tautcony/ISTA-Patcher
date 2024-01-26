// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.Core.Patcher;

using System.Text.Json;
using dnlib.DotNet;
using Serilog;

public interface IPatcher
{
    public List<Func<ModuleDefMD, int>> Patches { get; set; }

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
            Log.Fatal(ex, "Failed to load config file: {Reason}", ex.Message);
        }

        return null;
    }

    public static List<Func<ModuleDefMD, int>> GetPatches(params Type[] attributes)
    {
        return typeof(PatchUtils)
            .GetMethods()
            .Where(m => Array.Exists(attributes, attribute => m.GetCustomAttributes(attribute, false).Length > 0))
            .Select(m => (Func<ModuleDefMD, int>)Delegate.CreateDelegate(typeof(Func<ModuleDefMD, int>), m))
            .ToList();
    }
}
