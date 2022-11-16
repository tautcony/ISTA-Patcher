using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

namespace ISTA_Patcher
{
    internal enum PatchTypeEnum {
        BMW = 0,
        TOYOTA = 1
    }

    [Verb("patch", HelpText = "Patch application and library.")]
    class PatchOptions {
        [Option('t', "type", Default = PatchTypeEnum.BMW, HelpText = "Patch type, valid option: BMW, TOYOTA")]
        public PatchTypeEnum PatchType { get; set; }
        
        [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }

        [Value(1, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
        public string? TargetPath { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt integrity checklist.")]
    class DecryptOptions
    {
        [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
        public string? TargetPath { get; set; }
    }

    internal static class Patcher
    {
        private static readonly Func<AssemblyDefinition, bool>[] Patches =
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
        };

        private static readonly Func<AssemblyDefinition, bool>[] ToyotaPatches =
        {
            PatchUtils.PatchCommonFuncForIsta,
            PatchUtils.PatchPackageValidityService,
            PatchUtils.PatchToyotaWorker,
        };

        private static IEnumerable<string> BuildIndicator(IReadOnlyCollection<Func<AssemblyDefinition, bool>> patches)
        {
            return patches.Select(i => i.Method.Name[5..]).Reverse().ToList().Select((t, i) =>
                new string('│', patches.Count - 1 - i) + "└" + new string('─', i + 1) + t);
        }

        private static readonly string[] RequiredLibraries = {
            /*
            "RheingoldCoreContracts.dll",
            "RheingoldCoreFramework.dll"
            */
        };
        
        static void PatchISTA(string basePath, List<string> pendingPatchList, PatchOptions options, string outputDir = "patched")
        {
            if (!Directory.Exists(basePath))
            {
                Log.Fatal("Folder '{BasePath}' not found, exiting...", basePath);
                return;
            }

            foreach (var library in RequiredLibraries)
            {
                if (File.Exists(Path.Join(basePath, library))) continue;
                Log.Fatal("Required library '{Library}' not found, exiting...", library);
                return;
            }

            var validPatches = options.PatchType == PatchTypeEnum.BMW ? Patches : Patches.Concat(ToyotaPatches).ToArray();
            Log.Information("=== ISTA Patch Begin ===");
            var timer = Stopwatch.StartNew();
            var indentLength = pendingPatchList.Select(i => i.Length).Max() + 1;

            foreach (var pendingPatchItem in pendingPatchList)
            {
                var path = Path.Join(basePath, pendingPatchItem);
                var moddedDir = Path.Join(basePath, outputDir);
                var targetPath = Path.Join(moddedDir, pendingPatchItem);

                var indent = new string(' ', indentLength - pendingPatchItem.Length);
                if (!File.Exists(path))
                {
                    Log.Information("{Item}{Indent} [not found]", pendingPatchItem, indent);
                    continue;
                }

                Directory.CreateDirectory(moddedDir);

                try
                {
                    var module = PatchUtils.LoadModule(path);
                    var assembly = module.Assembly;
                    var isPatched = PatchUtils.CheckPatchedMark(assembly);
                    if (isPatched)
                    {
                        Log.Information("{Item}{Indent} [already patched]", pendingPatchItem, indent);
                        continue;
                    }

                    // Patch and print result
                    var result = validPatches.Select(patch => patch(assembly)).ToList();
                    isPatched = result.Any(i => i);
                    var resultStr = result.Aggregate("", (c, i) => c + (i ? "+" : "-"));


                    if (isPatched)
                    {
                        PatchUtils.SetPatchedMark(assembly);
                        assembly.Write(targetPath);
                        if (options.Deobfuscate)
                        {
                            try
                            {
                                var deobfTimer = Stopwatch.StartNew();

                                var deobfPath = targetPath + ".deobf";
                                PatchUtils.DeObfuscation(targetPath, deobfPath);
                                if (File.Exists(targetPath))
                                {
                                    File.Delete(targetPath);
                                }
                                File.Move(deobfPath, targetPath);

                                deobfTimer.Stop();
                                var timeStr = deobfTimer.ElapsedTicks > Stopwatch.Frequency
                                    ? $" in {deobfTimer.Elapsed:mm\\:ss}"
                                    : "";
                                Log.Information("{Item}{Indent}{Result} [patched][deobfuscate success{Time}]", pendingPatchItem, indent, resultStr, timeStr);
                            }
                            catch (ApplicationException ex)
                            {
                                Log.Information("{Item}{Indent}{Result} [patched][deobfuscate skipped]: {Reason}", pendingPatchItem, indent, resultStr, ex.Message);
                            }
                        }
                        else
                        {
                            Log.Information("{Item}{Indent}{Result} [patched]", pendingPatchItem, indent, resultStr);
                        }
                    }
                    else
                    {
                        Log.Information("{Item}{Indent}{Result} [skip]", pendingPatchItem, indent, resultStr);
                    }
                }
                catch (Exception ex)
                {
                    Log.Information("{Item}{Indent} [failed]: {Reason}", pendingPatchItem, indent, ex.Message);

                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
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

        private static string?[] LoadISTAList(string targetFilename, string guiBasePath)
        {
            // load file list from enc_cne_1.prg
            string?[] fileList = (IntegrityManager.DecryptFile(targetFilename) ?? new List<HashFileInfo>())
                                  .Select(f => f.FileName).ToArray();

            // or from directory ./TesterGUI/bin/Release
            if (fileList.Length == 0)
            {
                fileList = Directory.GetFiles(Path.Join(guiBasePath, "bin", "Release"))
                                    .Where(f => f.EndsWith(".exe") || f.EndsWith("dll"))
                                    .Select(Path.GetFileName).ToArray();
            }
            return fileList;
        }

        private static string?[] LoadISTAToyotaList(string guiBasePath)
        {
            var fileList = Directory.GetFiles(Path.Join(guiBasePath, "bin", "Release"))
                                                    .Where(f => f.EndsWith(".exe") || f.EndsWith("dll"))
                                                    .Select(Path.GetFileName).ToArray();
            return fileList;
        }

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
                var cwd = Path.GetDirectoryName(AppContext.BaseDirectory)!;
                var guiBasePath = Path.Join(opts.TargetPath, "TesterGUI");
                var targetFilename = Path.Join(opts.TargetPath, "Ecu", "enc_cne_1.prg");

                if (!Directory.Exists(guiBasePath))
                {
                    if (opts.PatchType == PatchTypeEnum.BMW && !File.Exists(targetFilename))
                    {
                        Log.Fatal("Folder structure not match, please check input path");
                        return -1;
                    }
                }

                // load exclude list that do not need to be processed
                string[]? excludeList = null;
                string[]? includeList = null;
                try
                {
                    using FileStream stream = new(Path.Join(cwd, "patchConfig.json"), FileMode.Open, FileAccess.Read);
                    var patchConfig = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream);
                    excludeList = patchConfig?.GetValueOrDefault("exclude");
                    includeList = opts.PatchType switch
                    {
                        PatchTypeEnum.BMW => patchConfig?.GetValueOrDefault("include"),
                        PatchTypeEnum.TOYOTA => patchConfig?.GetValueOrDefault("include.toyota"),
                        _ => Array.Empty<string>()
                    };
                }
                catch (Exception ex) when (
                    ex is FileNotFoundException or IOException or JsonException
                )
                {
                    Log.Fatal("Failed to load config file: {Reason}", ex.Message);
                }
                excludeList ??= Array.Empty<string>();
                includeList ??= Array.Empty<string>();

                var fileList = opts.PatchType switch
                {
                    PatchTypeEnum.BMW => LoadISTAList(targetFilename, guiBasePath),
                    PatchTypeEnum.TOYOTA => LoadISTAToyotaList(guiBasePath),
                    _ => Array.Empty<string>()
                };

                var patchList = includeList
                                .Union(fileList.Where(f => !excludeList.Contains(f)))
                                .Distinct()
                                .OrderBy(i=>i).ToList();

                var basePath = Path.Join(guiBasePath, "bin", "Release");
                PatchISTA(basePath, patchList!, opts);

                return 0;
            }

            static int RunDecryptAndReturnExitCode(DecryptOptions opts)
            {
                var targetFilename = Path.Join(opts.TargetPath, "Ecu", "enc_cne_1.prg");
                if (!File.Exists(targetFilename)) return -1;
                var fileList = IntegrityManager.DecryptFile(targetFilename);
                if (fileList == null) return -1;
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
    }
}
