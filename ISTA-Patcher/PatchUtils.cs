using Mono.Cecil;
using Mono.Cecil.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace ISTA_Patcher
{
    internal class PatchUtils
    {
        private static string _timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        public static AssemblyDefinition LoadAssembly(string fileName)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(fileName));
            var assembly = AssemblyDefinition.ReadAssembly(fileName,
                new ReaderParameters { AssemblyResolver = assemblyResolver, InMemory = true });
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
                return false;
            }

            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchCommonServiceWrapper(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                return false;
            }

            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchSecureAccessHelper(AssemblyDefinition assembly)
        {
            var IsCodeAccessPermitted = assembly.GetMethod(
                "BMW.iLean.CommonServices.Helper.SecureAccessHelper",
                "IsCodeAccessPermitted",
                "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean"
            );
            if (IsCodeAccessPermitted == null)
            {
                return false;
            }

            IsCodeAccessPermitted.EmptyingMethod();
            return true;
        }

        public static bool PatchLicenseWizardHelper(AssemblyDefinition assembly)
        {
            var DoLicenseCheck = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
                "DoLicenseCheck",
                "(System.String)System.Boolean"
            );
            if (DoLicenseCheck == null)
            {
                return false;
            }

            DoLicenseCheck.ReturnOneMethod();
            return true;
        }

        public static bool PatchVerifyAssemblyHelper(AssemblyDefinition assembly)
        {
            var VerifyStrongName = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
                "VerifyStrongName",
                "(System.String,System.Boolean)System.Boolean"
            );
            if (VerifyStrongName == null)
            {
                return false;
            }

            VerifyStrongName.ReturnOneMethod();
            return true;
        }

        public static bool PatchFscValidationClient(AssemblyDefinition assembly)
        {
            var IsValid = assembly.GetMethod(
                "BMW.TricTools.FscValidation.FscValidationClient",
                "IsValid",
                "(System.Byte[],System.Byte[])System.Boolean"
            );
            if (IsValid == null)
            {
                return false;
            }

            IsValid.ReturnOneMethod();
            return true;
        }
        
        public static bool PatchMainWindowViewModel(AssemblyDefinition assembly)
        {
            var CheckExpirationDate = assembly.GetMethod(
                "BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
                "CheckExpirationDate",
                "()System.Void"
            );
            if (CheckExpirationDate == null)
            {
                return false;
            }

            CheckExpirationDate.EmptyingMethod();
            return true;
        }

        public static bool PatchCommonFuncForIsta(AssemblyDefinition assembly)
        {
            var GetLicenseStatus = assembly.GetMethod(
                "Toyota.GTS.ForIsta.CommonFuncForIsta",
                "GetLicenseStatus",
                "()BMW.Rheingold.ToyotaLicenseHelper.ToyotaLicenseStatus"
            );
            if (GetLicenseStatus == null)
            {
                return false;
            }

            GetLicenseStatus.ReturnZeroMethod();
            return true;
        }

        public static bool PatchPackageValidityService(AssemblyDefinition assembly)
        {
            var CyclicExpirationDateCheck = assembly.GetMethod(
                "BMW.Rheingold.ISTAGUI.Controller.PackageValidityService",
                "CyclicExpirationDateCheck",
                "()System.Void"
            );
            if (CyclicExpirationDateCheck == null)
            {
                return false;
            }

            CyclicExpirationDateCheck.EmptyingMethod();
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
            var dateField = new FieldDefinition(
                "date",
                FieldAttributes.Private | FieldAttributes.Static,
                assembly.MainModule.ImportReference(typeof(string)))
            {
                Constant = _timestamp
            };
            var urlField = new FieldDefinition(
                "repo",
                FieldAttributes.Private | FieldAttributes.Static,
                assembly.MainModule.ImportReference(typeof(string)))
            {
                Constant = "https://github.com/tautcony/ISTA-Patcher"
            };

            patchedType.Fields.Add(dateField);
            patchedType.Fields.Add(urlField);
            assembly.MainModule.Types.Add(patchedType);
        }

        public static string DecryptString(string encrypted, int magic, int value)
        {
            var charArray = encrypted.ToCharArray();
            var key = magic + value;
            for (var i = 0; i < charArray.Length; i++)
            {
                var ch = charArray[i];

                var b1 = (byte)((ch & 0xff) ^ key++);
                var b2 = (byte)((ch >> 8) ^ key++);
                var orgCh = (char)(((uint)b1 << 8) | b2);
                charArray[i] = orgCh;
            }

            return string.Intern(new string(charArray));
        }
    }
}
