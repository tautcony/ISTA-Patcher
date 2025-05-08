// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

namespace ISTAlter.Core.Patcher.Provider;

using System.Reflection;
using dnlib.DotNet;
using ISTAlter.Utils;
using ZLinq;

public interface IPatcherProvider
{
    public List<PatchInfo> Patches { get; set; }

    public string[] GeneratePatchList(ISTAOptions.PatchOptions options)
    {
        var fileList = this.LoadFileList(options.TargetPath);

        var excludeList = options.Exclude ?? [];
        var includeList = options.Include ?? [];

        var patchList = includeList
            .Union(fileList.Except(excludeList, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return patchList;
    }

    public string?[] LoadFileList(string basePath);

    public static string?[] DefaultLoadFileList(string basePath)
    {
        var fileList = Directory.GetFiles(Constants.TesterGUIPath.Aggregate(basePath, Path.Join))
            .AsValueEnumerable()
            .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName).ToArray();
        return fileList;
    }

    public static List<PatchInfo> GetPatches(params Type[] attributes)
    {
        return typeof(PatchUtils)
            .GetMethods()
            .AsValueEnumerable()
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
