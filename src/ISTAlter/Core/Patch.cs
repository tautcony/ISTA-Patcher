// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2026 TautCony

namespace ISTAlter.Core;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        var indentLength = pendingPatchList.Select(i => i.Length).DefaultIfEmpty(0).Max() + 1;

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

        var attemptedPatches = patcherProvider.Patches.Where(p => p.AttemptedCount > 0).ToList();
        var appliedCount = attemptedPatches.Count(p => p.AppliedCount > 0);
        var attemptedCount = attemptedPatches.Count;
        var skippedCount = patcherProvider.Patches.Count - attemptedCount;
        var totalFunctions = patcherProvider.Patches.Sum(p => p.AppliedCount);

        const string green = "[32m";
        const string red = "[31m";
        const string reset = "[0m";
        var countColor = appliedCount == attemptedCount ? green : red;
        var counter = $"{countColor}{appliedCount}/{attemptedCount}{reset}";

        Log.Information(
            @"=== ISTA Patch Done in {Time:mm\:ss\.fff} [{Counter} patches, {Functions} functions, {Skipped} skipped] ===",
            timer.Elapsed,
            counter,
            totalFunctions,
            skippedCount);
    }

    private static void PatchSingleFile(string pendingPatchItem, string guiBasePath, int indentLength, IPatcherProvider patcherProvider, ISTAOptions.PatchOptions options)
    {
        var pendingPatchItemFullPath = pendingPatchItem.StartsWith('!')
            ? Path.Join(options.TargetPath, pendingPatchItem.Trim('!'))
            : Path.Join(guiBasePath, pendingPatchItem);

        // Normalize and validate path to prevent directory traversal attacks
        pendingPatchItemFullPath = Path.GetFullPath(pendingPatchItemFullPath);
        var expectedBasePath = Path.GetFullPath(
            pendingPatchItem.StartsWith('!') ? options.TargetPath : guiBasePath);

        // Ensure path is within the expected directory
        if (!pendingPatchItemFullPath.StartsWith(expectedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            Log.Error("Path traversal attempt detected: {Item}", pendingPatchItem);
            return;
        }

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
            // Handle restore mode: restore from backup and exit
            if (options.Restore)
            {
                if (File.Exists(bakFileFullPath))
                {
                    Log.Debug("Backup detected, restoring {Item}", pendingPatchItem);
                    File.Copy(bakFileFullPath, pendingPatchItemFullPath, overwrite: true);
                    Log.Information("{Item}{Indent}[RESTORED]", pendingPatchItem, indent);
                }
                else
                {
                    Log.Information("{Item}{Indent}[NO BACKUP]", pendingPatchItem, indent);
                }

                return;
            }

            using var module = PatchUtils.LoadModule(pendingPatchItemFullPath);
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
            var skipLibraries = options.SkipLibrary;
            var resultBuilder = new StringBuilder(patcherProvider.Patches.Count);
            var patchedFunctionCount = 0;
            isPatched = false;

            foreach (var patch in patcherProvider.Patches)
            {
                var libraryList = IPatcherProvider.ExtractLibrariesConfigFromAttribute(patch.Method);
                if (ShouldSkipPatch(skipLibraries, libraryList))
                {
                    Log.Warning("Skip patch {PatchName} due to library filter", patch.Method.Name);
                    resultBuilder.Append('-');
                    continue;
                }

                if (!PatchUtils.IsVersionInRange(module, patch.Method))
                {
                    resultBuilder.Append('-');
                    continue;
                }

                if (!PatchUtils.IsPatchApplicable(module, patch.Method))
                {
                    resultBuilder.Append('-');
                    continue;
                }

                patch.AddAttemptedCount();
                var patchedCount = patch.Delegator(module);
                patch.AddAppliedCount(patchedCount);
                if (patchedCount > 0)
                {
                    isPatched = true;
                    patchedFunctionCount += patchedCount;
                    resultBuilder.Append(patchedCount.ToString("X", CultureInfo.CurrentCulture));
                }
                else
                {
                    resultBuilder.Append('-');
                }
            }

            var resultStr = resultBuilder.ToString();

            // Check if at least one patch has been applied
            if (!isPatched)
            {
                Log.Information("{Item}{Indent}{Result} [NOP]", pendingPatchItem, indent, resultStr);
                return;
            }

            // Create backup only when patches will be saved
            if (!File.Exists(bakFileFullPath))
            {
                Log.Debug("Backup file {BakFileFullPath} does not exist, creating backup...", bakFileFullPath);
                File.Copy(pendingPatchItemFullPath, bakFileFullPath, overwrite: false);
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

            PatchUtils.SetPatchedMark(module);
            PatchUtils.SaveModule(module, patchedFileFullPath);

            Log.Debug("Patched file {PatchedFileFullPath} created", patchedFileFullPath);
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

        static bool ShouldSkipPatch(string[] skipLibraries, string[] libraryList)
        {
            foreach (var skipLibrary in skipLibraries)
            {
                if (Array.IndexOf(libraryList, skipLibrary) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static IEnumerable<string> BuildIndicator(List<PatchInfo> patches)
    {
        return patches
               .Select(p => (Name: FormatName(p.Method), Count: p.AppliedCount, Attempted: p.AttemptedCount))
               .Reverse()
               .Select((item, idx) =>
               {
                   var revIdx = patches.Count - 1 - idx;
                   var verticalBars = new string('│', revIdx);
                   var horizontalBars = new string('─', idx);
                   var label = item.Attempted == 0 ? "skip" : item.Count.ToString();
                   return $"{verticalBars}└{horizontalBars}>[{item.Name}: {label}]";
               });

        string FormatName(MethodInfo method)
        {
            var match = ActionNamePattern().Match(method.Name);
            return (match.Success ? match.Groups["name"].Value : method.Name).Replace("_", "::", StringComparison.Ordinal);
        }
    }

    [GeneratedRegex("(?<=Patch)(?<name>.*?)(?=>|$)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ActionNamePattern();
}
