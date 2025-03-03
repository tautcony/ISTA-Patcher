// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2025 TautCony

namespace ISTAlter.Core;

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using ISTAlter.Core.Patcher;
using ISTAlter.Core.Patcher.Provider;
using ISTAlter.Utils;
using Serilog;

public static partial class Patch
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public static string OutputDirName { get; set; } = "@ista-patched";

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public static string BakDirName { get; set; } = "@ista-backup";

    public static void PatchISTA(IPatcherProvider patcherProvider, ISTAOptions.PatchOptions options)
    {
        Log.Information("=== ISTA Patch Begin at {Time:yyyy-MM-ddTHH:mm:ss.fff} ===", DateTime.Now);
        var timer = Stopwatch.StartNew();

        var guiBasePath = Constants.TesterGUIPath.Aggregate(options.TargetPath, Path.Join);
        var pendingPatchList = patcherProvider.GeneratePatchList(options);
        var indentLength = pendingPatchList.Max(i => i.Length) + 1;

        var cts = new CancellationTokenSource();
        var factory = new TaskFactory(new ConcurrencyTaskScheduler(options.MaxDegreeOfParallelism));
        var tasks = pendingPatchList.Select(item => factory.StartNew(() => PatchSingleFile(item, guiBasePath, indentLength, patcherProvider, options), cts.Token));
        Task.WaitAll(tasks, cts.Token);

        foreach (var line in BuildIndicator(patcherProvider.Patches))
        {
            Log.Information("{Indent}{Line}", new string(' ', indentLength), line);
        }

        if (options.GenerateMockRegFile)
        {
            Log.Information("=== Registry file generating ===");
            RegistryUtils.GenerateMockRegFile(guiBasePath, options.Force);
        }

        timer.Stop();
        Log.Information(@"=== ISTA Patch Done in {Time:mm\:ss\.fff} ===", timer.Elapsed);
    }

    private static void PatchSingleFile(string pendingPatchItem, string guiBasePath, int indentLength, IPatcherProvider patcherProvider, ISTAOptions.PatchOptions options)
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
                string.Concat(Enumerable.Repeat("*", patcherProvider.Patches.Count)));
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
            var patcherVersion = PatchUtils.HavePatchedMark(module);
            var isPatched = patcherVersion != null;
            if (isPatched && !options.Force)
            {
                Log.Information(
                    "{Item}{Indent}{Result} [VER: {Version}]",
                    pendingPatchItem,
                    indent,
                    string.Concat(Enumerable.Repeat("*", patcherProvider.Patches.Count)),
                    patcherVersion);
                return;
            }

            // Patch and print result
            using var child = new SpanHandler(options.Transaction, pendingPatchItem);
            var result = patcherProvider.Patches.ConvertAll(patch =>
            {
                var libraryList = IPatcherProvider.ExtractLibrariesConfigFromAttribute(patch.Method);
                if (options.SkipLibrary.Intersect(libraryList, StringComparer.Ordinal).Any())
                {
                    Log.Warning("Skip patch {PatchName} due to library filter", patch.Method.Name);
                    return 0;
                }

                if (!PatchUtils.CheckPatchVersion(module, patch.Method))
                {
                    return 0;
                }

                var patchedCount = patch.Delegator(module);
                patch.AppliedCount += patchedCount;
                return patchedCount;
            });

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
                            return ResourceUtils.AddWatermark(stream, PatchUtils.GetCoefficients().GetString(12));
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
            Log.Information("{Item}{Indent}{Result} [FNC: {PatchedFunctionCount:00}]", pendingPatchItem, indent, resultStr, patchedFunctionCount);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Log.Information(
                "{Item}{Indent}{Result} [failed]: {Reason}",
                pendingPatchItem,
                indent,
                string.Concat(Enumerable.Repeat("*", patcherProvider.Patches.Count)),
                ex.Message);
            Log.Debug("ExceptionType: {ExceptionType}, StackTrace: {StackTrace}", ex.GetType().FullName, ex.StackTrace);

            if (File.Exists(patchedFileFullPath))
            {
                File.Delete(patchedFileFullPath);
            }
        }
    }

    private static IEnumerable<string> BuildIndicator(List<PatchInfo> patches)
    {
        return patches
               .Select(p => (Name: FormatName(p.Delegator), Count: p.AppliedCount))
               .Reverse()
               .Select((item, idx) =>
               {
                   var revIdx = patches.Count - 1 - idx;
                   var verticalBars = new string('│', revIdx);
                   var horizontalBars = new string('─', idx);
                   return $"{verticalBars}└{horizontalBars}>[{item.Name}: {item.Count}]";
               });

        string FormatName(Func<ModuleDefMD, int> func)
        {
            var match = ActionNamePattern().Match(func.Method.Name);
            return (match.Success ? match.Groups["name"].Value : func.Method.Name).Replace("_", "::", StringComparison.Ordinal);
        }
    }

    [GeneratedRegex("(?<=Patch)(?<name>.*?)(?=>|$)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ActionNamePattern();
}
