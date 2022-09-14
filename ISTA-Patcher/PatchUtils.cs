using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;

using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

namespace ISTA_Patcher
{
    internal static class PatchUtils
    {
        private static readonly string _timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        private static readonly ModuleContext _modCtx = ModuleDef.CreateModuleContext();
        private static readonly NewProcessAssemblyClientFactory _processAssemblyClientFactory = new ();
        
        public static ModuleDefMD LoadModule(string fileName)
        {
            var module = ModuleDefMD.Load(fileName, _modCtx);
            return module;
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
        
        public static bool PatchToyotaWorker(AssemblyDefinition assembly)
        {
            var VehicleIsValid = assembly.GetMethod(
                "BMW.Rheingold.Toyota.Worker.ToyotaWorker",
                "VehicleIsValid",
                "(System.String)System.Boolean"
            );
            if (VehicleIsValid == null)
            {
                return false;
            }

            VehicleIsValid.ReturnOneMethod();
            return true;
        }


        public static bool CheckPatchedMark(AssemblyDefinition assembly)
        {
            var patchedType = assembly.Modules.First().GetType("Patched.By.TC");
            return patchedType != null;
        }

        public static void SetPatchedMark(AssemblyDefinition assembly)
        {
            var module = assembly.Modules.FirstOrDefault();
            if (module == null)
            {
                return;
            }
            
            var patchedType = new TypeDefUser(
                "Patched.By", "TC",
                module.CorLibTypes.Object.TypeDefOrRef)
            {
                Attributes = TypeAttributes.Class | TypeAttributes.NestedPrivate
            };
            var dateField = new FieldDefUser(
                "date",
                new FieldSig(module.CorLibTypes.String),
                FieldAttributes.Private | FieldAttributes.Static
            )
            {
                Constant = new ConstantUser(_timestamp)
            };
            var urlField = new FieldDefUser(
                "repo",
                new FieldSig(module.CorLibTypes.String),
                FieldAttributes.Private | FieldAttributes.Static
                )
            {
                Constant = new ConstantUser("https://github.com/tautcony/ISTA-Patcher")
            };

            patchedType.Fields.Add(dateField);
            patchedType.Fields.Add(urlField);
            module.Types.Add(patchedType);
        }

        public static void DeObfuscation(string fileName, string newFileName)
        {
            var deobfuscatorInfo = new DeobfuscatorInfo();
            var file = new ObfuscatedFile(new ObfuscatedFile.Options()
            {
                ControlFlowDeobfuscation = true,
                Filename = fileName,
                NewFilename = newFileName,
                StringDecrypterType = DecrypterType.Static
            }, _modCtx, _processAssemblyClientFactory);

            file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
            file.DeobfuscateBegin();
            file.Deobfuscate();
            file.DeobfuscateEnd();
            file.Save();
        }
    }
}
