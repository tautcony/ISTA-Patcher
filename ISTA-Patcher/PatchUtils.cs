using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Serilog;
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

        public static bool PatchConfigurationService(AssemblyDefinition assembly)
        {
            void RewriteProperties(MethodDef method)
            {
                var instructions = method.Body.Instructions;

                var baseService = instructions.FindInstruction(OpCodes.Call, "com.bmw.psdz.api.Configuration BMW.Rheingold.Psdz.Services.ServiceBase`1<com.bmw.psdz.api.Configuration>::get_BaseService()");
                var getPSdZProperties = instructions.FindInstruction(OpCodes.Callvirt, "java.util.Properties com.bmw.psdz.api.Configuration::getPSdZProperties()");
                var setPSdZProperties = instructions.FindInstruction(OpCodes.Callvirt, "System.Void com.bmw.psdz.api.Configuration::setPSdZProperties(java.util.Properties)");
                var putProperty = instructions.FindInstruction(OpCodes.Call, "System.Void BMW.Rheingold.Psdz.Services.ConfigurationService::PutProperty(java.util.Properties,java.lang.String,java.lang.String)");
                var stringImplicit = instructions.FindInstruction(OpCodes.Call, "java.lang.String java.lang.String::op_Implicit(System.String)");

                if (baseService == null || getPSdZProperties == null || setPSdZProperties == null || putProperty == null ||
                    stringImplicit == null)
                {
                    Log.Warning("Can not patch ConfigurationService");
                    return;
                }
                var patchedMethod = new List<Instruction>
                {
                    // Properties pSdZProperties = base.BaseService.getPSdZProperties();
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, baseService.Operand as MemberRef),
                    Instruction.Create(OpCodes.Callvirt, getPSdZProperties.Operand as MemberRef),
                    Instruction.Create(OpCodes.Stloc_0),
                    // PutProperty(pSdZProperties, String.op_Implicit("DealerID"), String.op_Implicit("1234"));
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Ldstr, "DealerID"),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Ldstr, "1234"),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Call, putProperty.Operand as MethodDef),
                    // PutProperty(pSdZProperties, String.op_Implicit("PlantID"), String.op_Implicit("0"));
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Ldstr, "PlantID"),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Ldstr, "0"),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Call, putProperty.Operand as MethodDef),
                    // PutProperty(pSdZProperties, String.op_Implicit("ProgrammierGeraeteSeriennummer"), String.op_Implicit(programmierGeraeteSeriennummer));
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Ldstr, "ProgrammierGeraeteSeriennummer"),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Ldarg_3),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Call, putProperty.Operand as MethodDef),
                    // PutProperty(pSdZProperties, String.op_Implicit("Testereinsatzkennung"), String.op_Implicit(testerEinsatzKennung));
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Ldstr, "Testereinsatzkennung"),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Ldarg_S, method.Parameters[4]),
                    Instruction.Create(OpCodes.Call, stringImplicit.Operand as MemberRef),
                    Instruction.Create(OpCodes.Call, putProperty.Operand as MethodDef),
                    // base.BaseService.setPSdZProperties(pSdZProperties);
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, baseService.Operand as MemberRef),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Callvirt, setPSdZProperties.Operand as MemberRef),
                    Instruction.Create(OpCodes.Ret)
                };

                method.Body.Instructions.Clear();
                patchedMethod.ForEach(instruction => method.Body.Instructions.Add(instruction));
            }

            return PatchFunction(assembly,
                "BMW.Rheingold.Psdz.Services.ConfigurationService",
                "SetPsdzProperties",
                "(System.String,System.String,System.String,System.String)System.Void",
                RewriteProperties
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

        public static Instruction? FindInstruction(this IEnumerable<Instruction> list, OpCode opCode, string operandName)
        {
            foreach (var instruction in list)
            {
                if (instruction.OpCode != opCode)
                    continue;
                switch (instruction.Operand)
                {
                    case MemberRef memberRef when memberRef.FullName == operandName:
                        return instruction;
                    case MethodDef methodDef when methodDef.FullName == operandName:
                        return instruction;
                }
            }
            return null;
        }
    }
}
