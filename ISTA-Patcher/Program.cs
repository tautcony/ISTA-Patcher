// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022 TautCony
namespace ISTA_Patcher;

using System.Diagnostics;
using System.Text;
using CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

internal enum PatchTypeEnum
{
    BMW = 0,
    TOYOTA = 1,
}

[Verb("patch", HelpText = "Patch application and library.")]
internal class PatchOptions
{
    [Option('t', "type", Default = PatchTypeEnum.BMW, HelpText = "Patch type, valid option: BMW, TOYOTA")]
    public PatchTypeEnum PatchType { get; set; }

    [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
    public bool Deobfuscate { get; set; }

    [Value(1, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
    public string TargetPath { get; set; }
}

[Verb("decrypt", HelpText = "Decrypt integrity checklist.")]
internal class DecryptOptions
{
    [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
    public string? TargetPath { get; set; }
}

internal static class ISTAPatcher
{
    public static int Main(string[] args)
    {
        var levelSwitch = new LoggingLevelSwitch
        {
            MinimumLevel = LogEventLevel.Debug,
        };
        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.ControlledBy(levelSwitch)
                     .WriteTo.Console()
                     .CreateLogger();

        return Parser.Default.ParseArguments<PatchOptions, DecryptOptions>(args)
                     .MapResult(
                         (PatchOptions opts) => RunPatchAndReturnExitCode(opts),
                         (DecryptOptions opts) => RunDecryptAndReturnExitCode(opts),
                         errs => 1);

        static int RunPatchAndReturnExitCode(PatchOptions opts)
        {
            var guiBasePath = Path.Join(opts.TargetPath, "TesterGUI", "bin", "Release");
            var psdzBasePath = Path.Join(opts.TargetPath, "PSdZ", "host");

            if (!Directory.Exists(guiBasePath) || !Directory.Exists(psdzBasePath))
            {
                Log.Fatal("Folder structure not match, please check parameter");
                return -1;
            }

            IPatcher patcher = opts.PatchType switch
            {
                PatchTypeEnum.BMW => new BMWPatcher(),
                PatchTypeEnum.TOYOTA => new ToyotaPatcher(),
                _ => throw new NotImplementedException(),
            };

            PatchISTA(patcher, opts);
            return 0;
        }

        static int RunDecryptAndReturnExitCode(DecryptOptions opts)
        {
            var encryptedFileList = Path.Join(opts.TargetPath, "Ecu", "enc_cne_1.prg");
            if (!File.Exists(encryptedFileList))
            {
                return -1;
            }

            var fileList = IntegrityManager.DecryptFile(encryptedFileList);
            if (fileList == null)
            {
                return -1;
            }

            var filePathMaxLength = fileList.Select(f => f.FilePath.Length).Max();
            var hashMaxLength = fileList.Select(f => f.Hash.Length).Max();
            var markdownBuilder = new StringBuilder();
            markdownBuilder.AppendLine($"| {"FilePath".PadRight(filePathMaxLength)} | {"Hash(SHA256)".PadRight(hashMaxLength)} |");
            markdownBuilder.AppendLine($"| {"---".PadRight(filePathMaxLength)} | {"---".PadRight(hashMaxLength)} |");
            foreach (var fileInfo in fileList)
            {
                markdownBuilder.AppendLine($"| {fileInfo.FilePath.PadRight(filePathMaxLength)} | {fileInfo.Hash.PadRight(hashMaxLength)} |");
            }

            Log.Information("Markdown result:\n{Markdown}", markdownBuilder.ToString());
            return 0;
        }
    }

    private static IEnumerable<string> BuildIndicator(IReadOnlyCollection<Func<AssemblyDefinition, bool>> patches)
    {
        return patches.Select(i => i.Method.Name[5..]).Reverse().ToList().Select((t, i) =>
            new string('│', patches.Count - 1 - i) + "└" + new string('─', i + 1) + t);
    }

    private static void PatchISTA(IPatcher patcher, PatchOptions options, string outputDirName = "patched")
    {
        var guiBasePath = Path.Join(options.TargetPath, "TesterGUI", "bin", "Release");

        var validPatches = patcher.Patches;
        var pendingPatchList = patcher.GeneratePatchList(options.TargetPath);

        Log.Information("=== ISTA Patch Begin ===");
        var timer = Stopwatch.StartNew();
        var indentLength = pendingPatchList.Select(i => i.Length).Max() + 1;

        foreach (var pendingPatchItem in pendingPatchList)
        {
            var pendingPatchItemFullPath = pendingPatchItem.StartsWith("!") ? Path.Join(options.TargetPath, pendingPatchItem.Trim('!')) : Path.Join(guiBasePath, pendingPatchItem);

            var originalDirPath = Path.GetDirectoryName(pendingPatchItemFullPath);
            var patchedDirPath = Path.Join(originalDirPath, outputDirName);
            var patchedFileFullPath = Path.Join(patchedDirPath, Path.GetFileName(pendingPatchItem));

            var indent = new string(' ', indentLength - pendingPatchItem.Length);
            if (!File.Exists(pendingPatchItemFullPath))
            {
                Log.Information(
                    "{Item}{Indent}{Result} [not found]",
                    pendingPatchItem,
                    indent,
                    string.Concat(Enumerable.Repeat("*", validPatches.Length)));
                continue;
            }

            Directory.CreateDirectory(patchedDirPath);

            try
            {
                var module = PatchUtils.LoadModule(pendingPatchItemFullPath);
                var assembly = module.Assembly;
                var isPatched = PatchUtils.CheckPatchedMark(assembly);
                if (isPatched)
                {
                    Log.Information(
                        "{Item}{Indent}{Result} [already patched]",
                        pendingPatchItem,
                        indent,
                        string.Concat(Enumerable.Repeat("*", validPatches.Length)));
                    continue;
                }

                // Patch and print result
                var result = validPatches.Select(patch => patch(assembly)).ToList();
                isPatched = result.Any(i => i);
                var resultStr = result.Aggregate(string.Empty, (c, i) => c + (i ? "+" : "-"));

                // Check if patched
                if (!isPatched)
                {
                    Log.Information("{Item}{Indent}{Result} [skip]", pendingPatchItem, indent, resultStr);
                    continue;
                }

                PatchUtils.SetPatchedMark(assembly);
                assembly.Write(patchedFileFullPath);

                // Check if need to deobfuscate
                if (!options.Deobfuscate)
                {
                    Log.Information("{Item}{Indent}{Result} [patched]", pendingPatchItem, indent, resultStr);
                    continue;
                }

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
                        "{Item}{Indent}{Result} [patched][deobfuscate success{Time}]",
                        pendingPatchItem,
                        indent,
                        resultStr,
                        timeStr);
                }
                catch (ApplicationException ex)
                {
                    Log.Information(
                        "{Item}{Indent}{Result} [patched][deobfuscate skipped]: {Reason}",
                        pendingPatchItem,
                        indent,
                        resultStr,
                        ex.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Information(
                    "{Item}{Indent}{Result} [failed]: {Reason}",
                    pendingPatchItem,
                    indent,
                    string.Concat(Enumerable.Repeat("*", validPatches.Length)),
                    ex.Message);

                if (File.Exists(patchedFileFullPath))
                {
                    File.Delete(patchedFileFullPath);
                }
            }
        }

        foreach (var line in BuildIndicator(validPatches))
        {
            Log.Information("{Indent}{Line}", new string(' ', indentLength), line);
        }

        timer.Stop();
        Log.Information("=== ISTA Patch Done in {Time:mm\\:ss} ===", timer.Elapsed);
    }
}
