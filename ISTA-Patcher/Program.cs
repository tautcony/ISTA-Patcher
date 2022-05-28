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


    internal static class Patcher
    {
        static void PatchISTA(string basePath, IEnumerable<string> pendingPatchList, string outputDir = "modded")
        {
            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"Folder '{basePath}' not found, exiting...");
                return;
            }

            var requiredLibraryList = new[]
            {
                 "RheingoldCoreContracts.dll",
                 "RheingoldCoreFramework.dll"
            };

            foreach (var library in requiredLibraryList)
            {
                if (File.Exists(Path.Join(basePath, library))) continue;
                Console.WriteLine($"Required {library} not found, exiting...");
                return;
            }

            var IntegrityManagerList = new List<string>();
            var LicenseStatusCheckerList = new List<string>();
            var CheckSignatureList = new List<string>();
            var LicenseManagerList = new List<string>();
            var AOSLicenseManagerList = new List<string>();
            var IstaIcsServiceClientList = new List<string>();
            var CommonServiceWrapperList = new List<string>();
            var SecureAccessHelperList = new List<string>();
            var LicenseWizardHelperList = new List<string>();
            var VerifyAssemblyHelperList = new List<string>();
            var FscValidationClientList = new List<string>();

            Console.WriteLine("=== ISTA Patch Begin ===");
            foreach (var pendingPatchItem in pendingPatchList)
            {
                var path = Path.Join(basePath, pendingPatchItem);
                var moddedDir = Path.Join(basePath, outputDir);
                var targetPath = Path.Join(moddedDir, pendingPatchItem);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"{pendingPatchItem} [not found]");
                    continue;
                }

                if (!Directory.Exists(moddedDir))
                {
                    Directory.CreateDirectory(moddedDir);
                }

                Console.Write($"{pendingPatchItem} ");

                try
                {
                    var assembly = PatchUtils.LoadAssembly(path);
                    var isPatched = PatchUtils.CheckPatchedMark(assembly);
                    if (isPatched)
                    {
                        Console.WriteLine("[already patched]");
                        continue;
                    }

                    var patches = new List<KeyValuePair<Func<AssemblyDefinition, bool>, List<string>>>
                    {
                        new(PatchUtils.PatchIntegrityManager, IntegrityManagerList),
                        new(PatchUtils.PatchLicenseStatusChecker, LicenseStatusCheckerList),
                        new(PatchUtils.PatchCheckSignature, CheckSignatureList),
                        new(PatchUtils.PatchLicenseManager, LicenseManagerList),
                        new(PatchUtils.PatchAOSLicenseManager, AOSLicenseManagerList),
                        new(PatchUtils.PatchIstaIcsServiceClient, IstaIcsServiceClientList),
                        new(PatchUtils.PatchCommonServiceWrapper, CommonServiceWrapperList),
                        new(PatchUtils.PatchSecureAccessHelper, SecureAccessHelperList),
                        new(PatchUtils.PatchLicenseWizardHelper, LicenseWizardHelperList),
                        new(PatchUtils.PatchVerifyAssemblyHelper, VerifyAssemblyHelperList),
                        new(PatchUtils.PatchFscValidationClient, FscValidationClientList)
                    };

                    foreach (var pair in patches)
                    {
                        if (pair.Key(assembly))
                        {
                            isPatched = true;
                            Console.Write("+");
                            pair.Value.Add(pendingPatchItem);
                        }
                        else
                        {
                            Console.Write("-");
                        }
                    }

                    Console.Write(" ");
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
            return CommandLine.Parser.Default.ParseArguments<PatchOptions, DecryptOptions>(args)
                .MapResult(
                    (PatchOptions opts) => RunPatchAndReturnExitCode(opts),
                    (DecryptOptions opts) => RunDecryptAndReturnExitCode(opts),
                    errs => 1);


            static int RunPatchAndReturnExitCode(PatchOptions opts)
            {
                var cwd = Path.GetDirectoryName(AppContext.BaseDirectory)!;
                var guiBasePath = Path.Join(opts.TargetPath, "TesterGUI");
                var targetFilename = Path.Join(opts.TargetPath, "Ecu", "enc_cne_1.prg");
                if (!Directory.Exists(guiBasePath) || !File.Exists(targetFilename)) return 0;

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
        }
    }
}
