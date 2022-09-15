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
        private static readonly IDeobfuscatorContext _deobfuscatorContext = new DeobfuscatorContext();
        private static readonly NewProcessAssemblyClientFactory _processAssemblyClientFactory = new ();
        
        public static ModuleDefMD LoadModule(string fileName)
        {
            var module = ModuleDefMD.Load(fileName, _modCtx);
            return module;
        }

        private static bool PatchFunction(AssemblyDefinition assembly, string type, string name, string desc,
            Action<MethodDef> operation)
        {
            var function = assembly.GetMethod(type, name, desc);
            if (function == null)
            {
                return false;
            }
            operation(function);
            return true;
        }

        public static bool PatchIntegrityManager(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.SecurityAndLicense.IntegrityManager",
                ".ctor",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchLicenseStatusChecker(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
                "IsLicenseValid",
                "(BMW.Rheingold.CoreFramework.LicenseInfo,System.Boolean)BMW.Rheingold.CoreFramework.LicenseStatus",
                DnlibUtils.ReturnZeroMethod
            );
        }

        public static bool PatchCheckSignature(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
                "CheckSignature",
                "(System.String)System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchLicenseManager(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.CoreFramework.LicenseManager",
                "VerifyLicense",
                "(System.Boolean)System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchAOSLicenseManager(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.CoreFramework.LicenseAOSManager",
                "VerifyLicense",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchIstaIcsServiceClient(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
                "ValidateHost",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            ) && PatchFunction(assembly,
                "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
                "VerifyLicense",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchCommonServiceWrapper(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
                "VerifyLicense",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchSecureAccessHelper(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.iLean.CommonServices.Helper.SecureAccessHelper",
                "IsCodeAccessPermitted",
                "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
        }

        public static bool PatchLicenseWizardHelper(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
                "DoLicenseCheck",
                "(System.String)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
        }

        public static bool PatchVerifyAssemblyHelper(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
                "VerifyStrongName",
                "(System.String,System.Boolean)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
        }

        public static bool PatchFscValidationClient(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.TricTools.FscValidation.FscValidationClient",
                "IsValid",
                "(System.Byte[],System.Byte[])System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
        }

        public static bool PatchMainWindowViewModel(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
                "CheckExpirationDate",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchActivationCertificateHelper(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
                "IsInWhiteList",
                "(System.String,System.String,System.String)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            ) && PatchFunction(assembly,
                "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
                "IsWhiteListSignatureValid",
                "(System.String,System.String)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
        }

        public static bool PatchCertificateHelper(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.iLean.CommonServices.Helper.CertificateHelper",
                "ValidateCertificate",
                "(System.String)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
        }

        public static bool PatchCommonFuncForIsta(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "Toyota.GTS.ForIsta.CommonFuncForIsta",
                "GetLicenseStatus",
                "()BMW.Rheingold.ToyotaLicenseHelper.ToyotaLicenseStatus",
                DnlibUtils.ReturnZeroMethod
            );
        }

        public static bool PatchPackageValidityService(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.ISTAGUI.Controller.PackageValidityService",
                "CyclicExpirationDateCheck",
                "()System.Void",
                DnlibUtils.EmptyingMethod
            );
        }

        public static bool PatchToyotaWorker(AssemblyDefinition assembly)
        {
            return PatchFunction(assembly,
                "BMW.Rheingold.Toyota.Worker.ToyotaWorker",
                "VehicleIsValid",
                "(System.String)System.Boolean",
                DnlibUtils.ReturnTrueMethod
            );
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

            using var file = new ObfuscatedFile(new ObfuscatedFile.Options
            {
                ControlFlowDeobfuscation = true,
                Filename = fileName,
                NewFilename = newFileName,
                StringDecrypterType = DecrypterType.Static
            }, _modCtx, _processAssemblyClientFactory)
            {
                DeobfuscatorContext = _deobfuscatorContext
            };

            file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
            file.DeobfuscateBegin();
            file.Deobfuscate();
            file.DeobfuscateEnd();
            file.Save();
        }
    }
}
