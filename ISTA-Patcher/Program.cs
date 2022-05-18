using System.Text.Json;
using Mono.Cecil;


namespace ISTA_Patcher
{
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

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("no path provided");
                return;
            }

            var path = args[0];
            var cwd = Path.GetDirectoryName(AppContext.BaseDirectory)!;

            string[]? includeList = null;

            try
            {
                using FileStream stream = new(Path.Join(cwd, "patchConfig.json"), FileMode.Open, FileAccess.Read);
                var patchConfig = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream);
                includeList = patchConfig?.GetValueOrDefault("include");
            }
            catch (Exception ex) when (
                ex is FileNotFoundException or IOException or JsonException
            )
            {
                Console.WriteLine($"Failed to load config file: {ex.Message}");
            }

            if (includeList != null)
            {
                PatchISTA(path, includeList);
            }
            else
            {
                Console.WriteLine("config not found");
            }
        }
    }
}