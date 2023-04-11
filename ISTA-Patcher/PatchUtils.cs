// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher;

using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Serilog;
using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

internal static class PatchUtils
{
    private static readonly string _timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    private static readonly ModuleContext _modCtx = ModuleDef.CreateModuleContext();
    private static readonly IDeobfuscatorContext _deobfuscatorContext = new DeobfuscatorContext();
    private static readonly NewProcessAssemblyClientFactory _processAssemblyClientFactory = new();

    public static ModuleDefMD LoadModule(string fileName)
    {
        var module = ModuleDefMD.Load(fileName, _modCtx);
        return module;
    }

    private static bool PatchFunction(
        AssemblyDefinition assembly,
        string type,
        string name,
        string desc,
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
        return PatchFunction(
            assembly,
            "BMW.Rheingold.SecurityAndLicense.IntegrityManager",
            ".ctor",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchLicenseStatusChecker(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
            "IsLicenseValid",
            "(BMW.Rheingold.CoreFramework.LicenseInfo,System.Boolean)BMW.Rheingold.CoreFramework.LicenseStatus",
            DnlibUtils.ReturnZeroMethod
        );
    }

    public static bool PatchCheckSignature(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
            "CheckSignature",
            "(System.String)System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchLicenseManager(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "VerifyLicense",
            "(System.Boolean)System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchAOSLicenseManager(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.LicenseAOSManager",
            "VerifyLicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchIstaIcsServiceClient(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
            "ValidateHost",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        ) && PatchFunction(
            assembly,
            "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
            "VerifyLicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchCommonServiceWrapper(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
            "VerifyLicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchSecureAccessHelper(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.iLean.CommonServices.Helper.SecureAccessHelper",
            "IsCodeAccessPermitted",
            "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool PatchLicenseWizardHelper(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
            "DoLicenseCheck",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool PatchVerifyAssemblyHelper(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
            "VerifyStrongName",
            "(System.String,System.Boolean)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool PatchFscValidationClient(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.TricTools.FscValidation.FscValidationClient",
            "IsValid",
            "(System.Byte[],System.Byte[])System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool PatchMainWindowViewModel(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
            "CheckExpirationDate",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchActivationCertificateHelper(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsInWhiteList",
            "(System.String,System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) && PatchFunction(
            assembly,
            "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsWhiteListSignatureValid",
            "(System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool PatchCertificateHelper(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.iLean.CommonServices.Helper.CertificateHelper",
            "ValidateCertificate",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool PatchCommonFuncForIsta(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "Toyota.GTS.ForIsta.CommonFuncForIsta",
            "GetLicenseStatus",
            "()BMW.Rheingold.ToyotaLicenseHelper.ToyotaLicenseStatus",
            DnlibUtils.ReturnZeroMethod
        );
    }

    public static bool PatchPackageValidityService(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.ISTAGUI.Controller.PackageValidityService",
            "CyclicExpirationDateCheck",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static bool PatchToyotaWorker(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
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
            var getBaseService = method.FindInstruction(OpCodes.Call, "com.bmw.psdz.api.Configuration BMW.Rheingold.Psdz.Services.ServiceBase`1<com.bmw.psdz.api.Configuration>::get_BaseService()");
            var getPSdZProperties = method.FindInstruction(OpCodes.Callvirt, "java.util.Properties com.bmw.psdz.api.Configuration::getPSdZProperties()");
            var setPSdZProperties = method.FindInstruction(OpCodes.Callvirt, "System.Void com.bmw.psdz.api.Configuration::setPSdZProperties(java.util.Properties)");
            var putProperty = method.FindInstruction(OpCodes.Call, "System.Void BMW.Rheingold.Psdz.Services.ConfigurationService::PutProperty(java.util.Properties,java.lang.String,java.lang.String)");
            var stringImplicit = method.FindInstruction(OpCodes.Call, "java.lang.String java.lang.String::op_Implicit(System.String)");

            if (getBaseService == null || getPSdZProperties == null || setPSdZProperties == null || putProperty == null ||
                stringImplicit == null)
            {
                Log.Warning("instructions not found, can not patch ConfigurationService");
                return;
            }

            var patchedMethod = new[]
            {
                // Properties pSdZProperties = base.BaseService.getPSdZProperties();
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService.Operand as MemberRef),
                OpCodes.Callvirt.ToInstruction(getPSdZProperties.Operand as MemberRef),
                OpCodes.Stloc_0.ToInstruction(),

                // PutProperty(pSdZProperties, String.op_Implicit("DealerID"), String.op_Implicit("1234"));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("DealerID"),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Ldstr.ToInstruction("1234"),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Call.ToInstruction(putProperty.Operand as MethodDef),

                // PutProperty(pSdZProperties, String.op_Implicit("PlantID"), String.op_Implicit("0"));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("PlantID"),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Ldstr.ToInstruction("0"),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Call.ToInstruction(putProperty.Operand as MethodDef),

                // PutProperty(pSdZProperties, String.op_Implicit("ProgrammierGeraeteSeriennummer"), String.op_Implicit(programmierGeraeteSeriennummer));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("ProgrammierGeraeteSeriennummer"),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Ldarg_3.ToInstruction(),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Call.ToInstruction(putProperty.Operand as MethodDef),

                // PutProperty(pSdZProperties, String.op_Implicit("Testereinsatzkennung"), String.op_Implicit(testerEinsatzKennung));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("Testereinsatzkennung"),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Ldarg_S.ToInstruction(method.Parameters[4]),
                OpCodes.Call.ToInstruction(stringImplicit.Operand as MemberRef),
                OpCodes.Call.ToInstruction(putProperty.Operand as MethodDef),

                // base.BaseService.setPSdZProperties(pSdZProperties);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService.Operand as MemberRef),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setPSdZProperties.Operand as MemberRef),
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
            var property =
                method.Body.Variables.FirstOrDefault(variable => variable.Type.FullName == "java.util.Properties");
            if (property == null)
            {
                Log.Warning("Properties not found, patch for ConfigurationService may not workable");
                return;
            }

            method.Body.Variables.Clear();
            method.Body.Variables.Add(property);
        }

        return PatchFunction(
            assembly,
            "BMW.Rheingold.Psdz.Services.ConfigurationService",
            "SetPsdzProperties",
            "(System.String,System.String,System.String,System.String)System.Void",
            RewriteProperties
        );
    }

    public static Func<AssemblyDefinition, bool> GeneratePatchGetRSAPKCS1SignatureDeformatter(string modulus, string exponent)
    {
        return Patch;

        bool Patch(AssemblyDefinition assembly)
        {
            return PatchFunction(
                assembly,
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
                "GetRSAPKCS1SignatureDeformatter",
                "()System.Security.Cryptography.RSAPKCS1SignatureDeformatter",
                ReplaceParameters
            );

            void ReplaceParameters(MethodDef method)
            {
                var ldstr = method.Body.Instructions.Where(inst => inst.OpCode == OpCodes.Ldstr).ToList();
                if (ldstr.Count == 3)
                {
                    ldstr[0].Operand = modulus;
                    ldstr[1].Operand = exponent;
                }
                else
                {
                    Log.Warning("instruction ldstr count not match, can not patch LicenseStatusChecker");
                }
            }
        }
    }

    public static bool PatchInteractionAdministrationModel(AssemblyDefinition assembly)
    {
        void RewriteTitle(MethodDef method)
        {
            var instructions = method.Body.Instructions;
            var setTitle = instructions.FirstOrDefault(inst => inst.OpCode == OpCodes.Call && inst.Operand is MethodDef methodDef && methodDef.Name == "set_Title");
            if (setTitle == null)
            {
                return;
            }

            var setTitleIndex = instructions.IndexOf(setTitle);
            if (setTitleIndex < 0)
            {
                return;
            }

            var mod = method.Module;
            TypeRef stringRef = new TypeRefUser(mod, "System", "String", mod.CorLibTypes.AssemblyRef);
            MemberRef concatRef = new MemberRefUser(
                mod,
                "Concat",
                MethodSig.CreateStatic(mod.CorLibTypes.String, mod.CorLibTypes.String, mod.CorLibTypes.String),
                stringRef);

            instructions.Insert(setTitleIndex, OpCodes.Call.ToInstruction(concatRef));
            instructions.Insert(setTitleIndex, OpCodes.Ldstr.ToInstruction("(Patched By ISTA-Patcher)"));
        }

        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.Interaction.Models.InteractionAdministrationModel",
            ".ctor",
            "()System.Void",
            RewriteTitle
        );
    }

    public static bool PatchCompileTime(AssemblyDefinition assembly)
    {
        return PatchFunction(
            assembly,
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "LastCompileTimeIsInvalid",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        ) && PatchFunction(
            assembly,
            "BMW.Rheingold.ExternalToolLicense.ServiceProgramCompilerLicense",
            "CheckLicenseExpiration",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    public static bool HavePatchedMark(AssemblyDefinition assembly)
    {
        var patchedType = assembly.Modules.First().GetType("Patched.By.TC");
        return patchedType != null;
    }

    public static void SetPatchedMark(AssemblyDefinition assembly)
    {
        var module = assembly.Modules.FirstOrDefault();
        if (module == null || HavePatchedMark(assembly))
        {
            return;
        }

        var patchedType = new TypeDefUser(
            "Patched.By",
            "TC",
            module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = TypeAttributes.Class | TypeAttributes.NestedPrivate,
        };
        var dateField = new FieldDefUser(
            "date",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(_timestamp),
        };
        var urlField = new FieldDefUser(
            "repo",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser("https://github.com/tautcony/ISTA-Patcher"),
        };
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (version == null)
        {
            version = new Version("0.0.0.0");
        }

        var versionField = new FieldDefUser(
            "version",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(version.ToString()),
        };

        patchedType.Fields.Add(dateField);
        patchedType.Fields.Add(urlField);
        patchedType.Fields.Add(versionField);
        module.Types.Add(patchedType);
    }

    public static void DeObfuscation(string fileName, string newFileName)
    {
        var deobfuscatorInfo = new DeobfuscatorInfo();

        using var file = new ObfuscatedFile(
            new ObfuscatedFile.Options
        {
            ControlFlowDeobfuscation = true,
            Filename = fileName,
            NewFilename = newFileName,
            StringDecrypterType = DecrypterType.Static,
        },
            _modCtx,
            _processAssemblyClientFactory)
        {
            DeobfuscatorContext = _deobfuscatorContext,
        };

        file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
        file.DeobfuscateBegin();
        file.Deobfuscate();
        file.DeobfuscateEnd();
        file.Save();
    }
}
