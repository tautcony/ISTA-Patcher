// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Core;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using de4dot.code;
using dnlib.DotNet;
using ISTAlter.Core.Patcher;
using ISTAlter.Utils;
using Serilog;

public static partial class Patch
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public static string OutputDirName { get; set; } = "@ista-patched";

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public static string BakDirName { get; set; } = "@ista-backup";

    public static void PatchISTA(IPatcher patcher, ISTAOptions.PatchOptions options)
    {
        Log.Information("=== ISTA Patch Begin ===");
        var timer = Stopwatch.StartNew();

        var guiBasePath = Constants.TesterGUIPath.Aggregate(options.TargetPath, Path.Join);
        var pendingPatchList = patcher.GeneratePatchList(options.TargetPath);
        var indentLength = pendingPatchList.Select(i => i.Length).Max() + 1;
        var patchAppliedCount = new int[patcher.Patches.Count];

        TheAssemblyResolver.Instance.AddSearchDirectory(guiBasePath);

        var cts = new CancellationTokenSource();
        var factory = new TaskFactory(new ConcurrencyTaskScheduler(options.MaxDegreeOfParallelism));
        var tasks = pendingPatchList.Select(item => factory.StartNew(() => PatchSingleFile(item, patchAppliedCount, guiBasePath, indentLength, patcher, options), cts.Token));
        Task.WaitAll(tasks, cts.Token);

        foreach (var line in BuildIndicator(patcher.Patches, patchAppliedCount))
        {
            Log.Information("{Indent}{Line}", new string(' ', indentLength), line);
        }

        if (options.GenerateMockRegFile)
        {
            Log.Information("=== Registry file generating ===");
            PatchUtils.GenerateMockRegFile(guiBasePath, options.Force);
        }

        timer.Stop();
        Log.Information("=== ISTA Patch Done in {Time:mm\\:ss} ===", timer.Elapsed);
    }

    private static void PatchSingleFile(string pendingPatchItem, int[] patchAppliedCount, string guiBasePath, int indentLength, IPatcher patcher, ISTAOptions.PatchOptions options)
    {
        var pendingPatchItemFullPath = pendingPatchItem.StartsWith('!')
            ? Path.Join(options.TargetPath, pendingPatchItem.Trim('!'))
            : Path.Join(guiBasePath, pendingPatchItem);

        var originalDirPath = Path.GetDirectoryName(pendingPatchItemFullPath);
        var patchedDirPath = Path.Join(originalDirPath, OutputDirName);
        var patchedFileFullPath = Path.Join(patchedDirPath, Path.GetFileName(pendingPatchItem));
        var bakDirPath = Path.Join(originalDirPath, BakDirName);
        var bakFileFullPath = Path.Join(bakDirPath, Path.GetFileName(pendingPatchItem));

        if (File.Exists(patchedFileFullPath))
        {
            File.Delete(patchedFileFullPath);
        }

        var indent = new string(' ', indentLength - pendingPatchItem.Length);
        if (!File.Exists(pendingPatchItemFullPath))
        {
            Log.Information(
                "{Item}{Indent}{Result} [404]",
                pendingPatchItem,
                indent,
                string.Concat(Enumerable.Repeat("*", patcher.Patches.Count)));
            return;
        }

        Directory.CreateDirectory(patchedDirPath);
        Directory.CreateDirectory(bakDirPath);

        try
        {
            if (options.Restore && File.Exists(bakFileFullPath))
            {
                Log.Debug("Backup detected, restoring {Item}", pendingPatchItem);
                File.Copy(bakFileFullPath, pendingPatchItemFullPath, overwrite: true);
            }

            var module = PatchUtils.LoadModule(pendingPatchItemFullPath);
            TheAssemblyResolver.Instance.AddModule(module);
            var patcherVersion = PatchUtils.HavePatchedMark(module);
            var isPatched = patcherVersion != null;
            if (isPatched && !options.Force)
            {
                Log.Information(
                    "{Item}{Indent}{Result} [VER: {Version}]",
                    pendingPatchItem,
                    indent,
                    string.Concat(Enumerable.Repeat("*", patcher.Patches.Count)),
                    patcherVersion);
                return;
            }

            // Patch and print result
            var result = patcher.Patches.Select(patch =>
            {
                var libraryList = IPatcher.ExtractLibrariesConfigFromAttribute(patch.Method);
                return options.SkipLibrary.Intersect(libraryList, StringComparer.Ordinal).Any() ? 0 : patch.Delegater(module);
            }).ToList();
            result.Select((item, index) => (item, index)).ToList()
                  .ForEach(patch => patchAppliedCount[patch.index] += patch.item);

            isPatched = result.Exists(i => i > 0);
            var resultStr = string.Concat(result.Select(i => i > 0 ? i.ToString("X", CultureInfo.CurrentCulture) : "-"));

            // Check if at least one patch has been applied
            if (!isPatched)
            {
                Log.Information("{Item}{Indent}{Result} [NOP]", pendingPatchItem, indent, resultStr);
                return;
            }

            /*
            if (module.Name == "ISTAGUI.exe")
            {
                ResourceUtils.UpdateResource(
                    module,
                    "ISTAGUI.g.resources",
                    "grafik/png/ista_logo.png",
                    entry =>
                    {
                        if (entry.Value is Stream stream)
                        {
                            return ResourceUtils.AddWatermark(stream, PatchUtils.Config[..PatchUtils.Config.LastIndexOf(' ')]);
                        }

                        return null;
                    });
            }
            */

            if (!File.Exists(bakFileFullPath))
            {
                Log.Debug("Backup file {BakFileFullPath} does not exist, copy...", bakFileFullPath);
                File.Copy(pendingPatchItemFullPath, bakFileFullPath, overwrite: false);
            }

            PatchUtils.SetPatchedMark(module);
            PatchUtils.SaveModule(module, patchedFileFullPath);

            Log.Debug("Patched file {PatchedFileFullPath} created", patchedFileFullPath);
            var patchedFunctionCount = result.Aggregate(0, (c, i) => c + i);

            // Check if assembly need to be deobfuscated
            if (!options.Deobfuscate)
            {
                Log.Information("{Item}{Indent}{Result} [FNC: {PatchedFunctionCount:00}]", pendingPatchItem, indent, resultStr, patchedFunctionCount);
                return;
            }

            DeObfuscateSingleFile(pendingPatchItem, patchedFileFullPath, patchedFunctionCount, indent, resultStr);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Log.Information(
                "{Item}{Indent}{Result} [failed]: {Reason}",
                pendingPatchItem,
                indent,
                string.Concat(Enumerable.Repeat("*", patcher.Patches.Count)),
                ex.Message);
            Log.Debug("ExceptionType: {ExceptionType}, StackTrace: {StackTrace}", ex.GetType().FullName, ex.StackTrace);

            if (File.Exists(patchedFileFullPath))
            {
                File.Delete(patchedFileFullPath);
            }
        }
    }

    private static void DeObfuscateSingleFile(string pendingPatchItem, string patchedFileFullPath, int patchedFunctionCount, string indent, string resultStr)
    {
        try
        {
            var deobfTimer = Stopwatch.StartNew();

            var deobfPath = patchedFileFullPath + ".deobf";
            PatchUtils.DeObfuscation(patchedFileFullPath, deobfPath);
            if (File.Exists(patchedFileFullPath))
            {
                File.Delete(patchedFileFullPath);
            }

            File.Move(deobfPath, patchedFileFullPath);

            deobfTimer.Stop();
            var timeStr = deobfTimer.ElapsedTicks > Stopwatch.Frequency
                ? $" in {deobfTimer.Elapsed:mm\\:ss}"
                : string.Empty;
            Log.Information(
                "{Item}{Indent}{Result} [{PatchedFunctionCount} func patched][deobfuscate success{Time}]",
                pendingPatchItem,
                indent,
                resultStr,
                patchedFunctionCount,
                timeStr);
        }
        catch (ApplicationException ex)
        {
            if (!string.Equals(ex.Message, "Could not detect obfuscator!", StringComparison.Ordinal))
            {
                SentrySdk.CaptureException(ex);
            }

            Log.Information(
                "{Item}{Indent}{Result} [{PatchedFunctionCount} func patched][deobfuscate skipped]: {Reason}",
                pendingPatchItem,
                indent,
                resultStr,
                patchedFunctionCount,
                ex.Message);
        }
    }

    private static IEnumerable<string> BuildIndicator(List<(Func<ModuleDefMD, int> Delegater, MethodInfo Method)> patches, int[] counting)
    {
        return patches
               .Select(p => FormatName(p.Delegater))
               .Reverse()
               .AsEnumerable()
               .Select((name, idx) =>
               {
                   var revIdx = patches.Count - 1 - idx;
                   return $"{new string('│', revIdx)}└{new string('─', idx)}>[{name}: {counting[revIdx]}]";
               });

        string FormatName(Func<ModuleDefMD, int> func)
        {
            var name = func.Method.Name;
            var match = ActionNamePattern().Match(name);
            if (match.Success)
            {
                name = match.Groups[1].Value;
            }

            return name.StartsWith("Patch", StringComparison.Ordinal) ? name[5..] : name;
        }
    }

    [GeneratedRegex("^<([^>]+)>", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ActionNamePattern();
}
