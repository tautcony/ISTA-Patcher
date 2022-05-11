using System.Reflection;
using System.Text.Json;


namespace ISTA_Patcher
{
    class Patcher
    {
        static void PatchISTA(string basePath, string[] pendingPatchList, string outputDir = "modded")
        {
            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"{basePath} not found");
                return;
            }
            var requiredLibraryList = new[]
            {
                "RheingoldCoreContracts.dll",
                "RheingoldCoreFramework.dll"
            };

            foreach (var library in requiredLibraryList)
            {
                if (!File.Exists(Path.Join(basePath, library)))
                {
                    Console.WriteLine($"{library} not found");
                    return;
                }
            }

            var IntegrityManagerList = new List<string>();
            var LicenseStatusCheckerList = new List<string>();
            var CheckSignatureList = new List<string>();
            var LicenseManagerList = new List<string>();
            var AOSLicenseManagerList = new List<string>();
            var IstaIcsServiceClientList = new List<string>();
            var ICSList = new List<string>();

            Console.WriteLine("=== ISTA Patch Begin ===");
            foreach (var pendingPatchItem in pendingPatchList)
            {
                var path = Path.Join(basePath, pendingPatchItem);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"{pendingPatchItem} [not found]");
                    continue;
                }
                Console.Write($"{pendingPatchItem} ");

                try
                {
                    var assembly = PatchUtils.LoadAssembly(path);
                    var isPatched = PatchUtils.CheckPatchedMark(assembly);
                    if (isPatched)
                    {
                        Console.WriteLine($"[already patched]");
                        continue;
                    }

                    if (PatchUtils.PatchIntegrityManager(assembly)) { isPatched = true; IntegrityManagerList.Add(pendingPatchItem); }
                    if (PatchUtils.PatchLicenseStatusChecker(assembly)) { isPatched = true; LicenseStatusCheckerList.Add(pendingPatchItem); }
                    if (PatchUtils.PatchCheckSignature(assembly)) { isPatched = true; CheckSignatureList.Add(pendingPatchItem); }
                    if (PatchUtils.PatchLicenseManager(assembly)) { isPatched = true; LicenseManagerList.Add(pendingPatchItem); }
                    if (PatchUtils.PatchAOSLicenseManager(assembly)) { isPatched = true; AOSLicenseManagerList.Add(pendingPatchItem); }
                    if (PatchUtils.PatchIstaIcsServiceClient(assembly)) { isPatched = true; IstaIcsServiceClientList.Add(pendingPatchItem); }
                    if (PatchUtils.PatchICS(assembly)) { isPatched = true; ICSList.Add(pendingPatchItem); }


                    if (isPatched)
                    {
                        Console.WriteLine("[patched]");
                        PatchUtils.SetPatchedMark(assembly);
                        var moddedDir = Path.Join(basePath, outputDir);
                        if (!Directory.Exists(moddedDir))
                        {
                            Directory.CreateDirectory(moddedDir);
                        }
                        assembly.Write(Path.Join(moddedDir, pendingPatchItem));
                    }
                    else
                    {
                        Console.WriteLine("[skip]");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[failed]: {ex.Message}");
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
            string path = args[0];
            string cwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            string[]? includeList = null;

            try
            {
                using FileStream stream = new(Path.Join(cwd, "patchConfig.json"), FileMode.Open, FileAccess.Read);
                Dictionary<string, string[]>? patchConfig = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream);
                includeList = patchConfig?.GetValueOrDefault("include");
            }
            catch(Exception ex) when (
                ex is FileNotFoundException ||
                ex is IOException ||
                ex is JsonException
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
                Console.WriteLine("list not found");
            }
        }
    }
}
