using Mono.Cecil;

namespace ISTA_Patcher
{
    internal class PatchUtils
    {
        public static AssemblyDefinition LoadAssembly(string fileName)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(fileName));
            assemblyResolver.AddSearchDirectory("C:/Windows/Microsoft.NET/Framework64/v4.0.30319");
            var assembly = AssemblyDefinition.ReadAssembly(fileName, new ReaderParameters() { AssemblyResolver = assemblyResolver, InMemory = true });
            return assembly;
        }

        public static bool PatchIntegrityManager(AssemblyDefinition assembly)
        {
            var integrityManagerConstructor = assembly.GetMethod(
                "BMW.Rheingold.SecurityAndLicense.IntegrityManager",
                ".ctor",
                "()System.Void"
            );
            if (integrityManagerConstructor == null)
            {
                // Console.WriteLine($"{nameof(integrityManagerConstructor)} not found, skiping this...");
                return false;
            }
            integrityManagerConstructor.EmptyingMethod();
            return true;
        }

        public static bool PatchLicenseStatusChecker(AssemblyDefinition assembly)
        {
            var isLicenseValid = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
                "IsLicenseValid",
                "(BMW.Rheingold.CoreFramework.LicenseInfo,System.Boolean)BMW.Rheingold.CoreFramework.LicenseStatus"
            );
            if (isLicenseValid == null)
            {
                // Console.WriteLine($"{nameof(isLicenseValid)} not found, skiping this...");
                return false;
            }
            isLicenseValid.ReturnZeroMethod();
            return true;
        }

        public static bool PatchCheckSignature(AssemblyDefinition assembly)
        {
            var CheckSignature = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
                "CheckSignature",
                "(System.String)System.Void"
            );
            if (CheckSignature == null)
            {
                // Console.WriteLine($"{nameof(CheckSignature)} not found, skiping this...");
                return false;
            }
            CheckSignature.EmptyingMethod();
            return true;
        }

        public static bool PatchLicenseManager(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManager",
                "VerifyLicense",
                "(System.Boolean)System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchAOSLicenseManager(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseAOSManager",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchIstaIcsServiceClient(AssemblyDefinition assembly)
        {
            var ValidateHost = assembly.GetMethod(
                "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
                "ValidateHost",
                "()System.Void"
            );
            if (ValidateHost == null)
            {
                // Console.WriteLine($"{nameof(ValidateHost)} not found, skiping this...");
                return false;
            }
            ValidateHost.EmptyingMethod();
            var VerifyLicense = assembly.GetMethod(
                "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchICS(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool CheckPatchedMark(AssemblyDefinition assembly)
        {
            var patchedType = assembly.MainModule.GetType("Patched.By.TC");
            return patchedType != null;
        }

        public static void SetPatchedMark(AssemblyDefinition assembly)
        {
            var patchedType = new TypeDefinition(
                            "Patched.By", "TC",
                            TypeAttributes.NestedPrivate,
                            assembly.MainModule.ImportReference(typeof(object)));
            var dateField = new FieldDefinition("date", FieldAttributes.Private | FieldAttributes.Static, assembly.MainModule.ImportReference(typeof(string)))
            {
                Constant = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            patchedType.Fields.Add(dateField);
            assembly.MainModule.Types.Add(patchedType);
        }
    }
}
