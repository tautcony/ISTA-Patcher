using System.Text.Json;
using CommandLine;
using Mono.Cecil;


namespace ISTA_Patcher
{
    [Verb("patch", HelpText = "Patch application and library.")]
    class PatchOptions {
        [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
        public string? TargetPath { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt integrity checklist.")]
    class DecryptOptions
    {
        [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
        public string? TargetPath { get; set; }
    }
    
    [Verb("decode", HelpText = "Decode inline string.")]
    class DecodeOptions
    {
        [Value(0, MetaName = "Application path", Required = true, HelpText = "Path for Application")]
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
        };
            
        private static readonly string[] RequiredLibraries = {
            "RheingoldCoreContracts.dll",
            "RheingoldCoreFramework.dll"
        };
        
        static void PatchISTA(string basePath, IEnumerable<string> pendingPatchList, string outputDir = "patched")
        {
            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"Folder '{basePath}' not found, exiting...");
                return;
            }

            foreach (var library in RequiredLibraries)
            {
                if (File.Exists(Path.Join(basePath, library))) continue;
                Console.WriteLine($"Required library '{library}' not found, exiting...");
                return;
            }

            Console.WriteLine("=== ISTA Patch Begin ===");
            foreach (var pendingPatchItem in pendingPatchList)
            {
                var path = Path.Join(basePath, pendingPatchItem);
                var moddedDir = Path.Join(basePath, outputDir);
                var targetPath = Path.Join(moddedDir, pendingPatchItem);
                Console.Write($"{pendingPatchItem} ");
                if (!File.Exists(path))
                {
                    Console.WriteLine(" [not found]");
                    continue;
                }

                Directory.CreateDirectory(moddedDir);

                try
                {
                    var assembly = PatchUtils.LoadAssembly(path);
                    var isPatched = PatchUtils.CheckPatchedMark(assembly);
                    if (isPatched)
                    {
                        Console.WriteLine("[already patched]");
                        continue;
                    }

                    // Patch and print result
                    var result = Patches.Select(patch => patch(assembly)).ToList();
                    isPatched = result.Any(i => i);
                    Console.Write(result.Aggregate("", (c, i) => c + (i ? "+" : "-")) + " ");
                    

                    if (isPatched)
                    {
                        // PatchUtils.DecryptParameter(assembly);
                        Console.Write("[patched]");
                        PatchUtils.SetPatchedMark(assembly);
                        assembly.Write(targetPath);
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("[skip]");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[failed]: {ex.Message}");

                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                }
            }

            Console.WriteLine("=== ISTA Patch Done ===");
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<PatchOptions, DecryptOptions, DecodeOptions>(args)
                .MapResult(
                    (PatchOptions opts) => RunPatchAndReturnExitCode(opts),
                    (DecryptOptions opts) => RunDecryptAndReturnExitCode(opts),
                    (DecodeOptions opts) => RunDecodeAndReturnExitCode(opts),
                    errs => 1);


            static int RunPatchAndReturnExitCode(PatchOptions opts)
            {
                var cwd = Path.GetDirectoryName(AppContext.BaseDirectory)!;
                var guiBasePath = Path.Join(opts.TargetPath, "TesterGUI");
                var targetFilename = Path.Join(opts.TargetPath, "Ecu", "enc_cne_1.prg");
                if (!Directory.Exists(guiBasePath) || !File.Exists(targetFilename))
                {
                    Console.WriteLine("Folder structure not match, please check input path");
                    return -1;
                }

                // load exclude list that do not need to be processed
                string[]? excludeList = null;
                try
                {
                    using FileStream stream = new(Path.Join(cwd, "patchConfig.json"), FileMode.Open, FileAccess.Read);
                    var patchConfig = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream);
                    excludeList = patchConfig?.GetValueOrDefault("exclude");
                }
                catch (Exception ex) when (
                    ex is FileNotFoundException or IOException or JsonException
                )
                {
                    Console.WriteLine($"Failed to load config file: {ex.Message}");
                }
                excludeList ??= Array.Empty<string>();
                
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
                var includeList = fileList.Where(f => !excludeList.Contains(f));

                var basePath = Path.Join(guiBasePath, "bin", "Release");
                PatchISTA(basePath, includeList!);

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
                Console.WriteLine($"| {"FilePath".PadRight(filePathMaxLength)} | {"Hash".PadRight(hashMaxLength)} |");
                Console.WriteLine($"| {"---".PadRight(filePathMaxLength)} | {"---".PadRight(hashMaxLength)} |");
                foreach (var fileInfo in fileList)
                {
                    Console.WriteLine($"| {fileInfo.FilePath.PadRight(filePathMaxLength)} | {fileInfo.Hash.PadRight(hashMaxLength)} |");
                }
                return 0;
            }

            static int RunDecodeAndReturnExitCode(DecodeOptions opts)
            {
                if (!File.Exists(opts.TargetPath))
                {
                    return -1;
                }
                var assembly = PatchUtils.LoadAssembly(opts.TargetPath);
                PatchUtils.DecryptParameter(assembly);
                assembly.Write(opts.TargetPath + ".result");
                return 0;
            }
        }
    }
}
