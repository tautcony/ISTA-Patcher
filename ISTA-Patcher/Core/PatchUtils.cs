// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable CommentTypo, StringLiteralTypo, IdentifierTypo, InconsistentNaming, UnusedMember.Global
namespace ISTA_Patcher.Core;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Serilog;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the main part of the patching logic.
/// </summary>
internal static partial class PatchUtils
{
    [ValidationPatch]
    public static int PatchLicenseStatusChecker(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
            "IsLicenseValid",
            "(BMW.Rheingold.CoreFramework.LicenseInfo,System.Boolean)BMW.Rheingold.CoreFramework.LicenseStatus",
            DnlibUtils.ReturnZeroMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseWizardHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
            "DoLicenseCheck",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "VerifyLicense",
            "(System.Boolean)System.Void",
            DnlibUtils.EmptyingMethod
        ) + module.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "CheckRITALicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        ) + module.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "LastCompileTimeIsInvalid",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseHelper",
            "IsVehicleLockedDown",
            "(BMW.Rheingold.CoreFramework.DatabaseProvider.Vehicle)System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [ValidationPatch]
    public static int PatchCommonServiceWrapper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
            "VerifyLicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [ValidationPatch]
    public static int PatchSecureAccessHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.iLean.CommonServices.Helper.SecureAccessHelper",
            "IsCodeAccessPermitted",
            "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchFscValidationClient(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.TricTools.FscValidation.FscValidationClient",
            "IsValid",
            "(System.Byte[],System.Byte[])System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchMainWindowViewModel(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
            "CheckExpirationDate",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [ValidationPatch]
    public static int PatchActivationCertificateHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsInWhiteList",
            "(System.String,System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchFunction(
            "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsWhiteListSignatureValid",
            "(System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EssentialPatch]
    public static int PatchIntegrityManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.SecurityAndLicense.IntegrityManager",
            ".ctor",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchVerifyAssemblyHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
            "VerifyStrongName",
            "(System.String,System.Boolean)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EssentialPatch]
    public static int PatchIstaIcsServiceClient(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
            "ValidateHost",
            "()System.Void",
            RemovePublicKeyCheck
        );

        void RemovePublicKeyCheck(MethodDef method)
        {
            var getProcessesByName = DnlibUtils.BuildCall(module, typeof(System.Diagnostics.Process), "GetProcessesByName", typeof(System.Diagnostics.Process[]), new[] { typeof(string) });
            var firstOrDefault = method.FindOperand<MethodSpec>(OpCodes.Call, "System.Diagnostics.Process System.Linq.Enumerable::FirstOrDefault<System.Diagnostics.Process>(System.Collections.Generic.IEnumerable`1<System.Diagnostics.Process>)");
            var invalidOperationException = DnlibUtils.BuildCall(module, typeof(InvalidOperationException), ".ctor", typeof(void), new[] { typeof(string) });
            if (getProcessesByName == null || firstOrDefault == null || invalidOperationException == null)
            {
                Log.Warning("Required instructions not found, can not patch IstaIcsServiceClient::ValidateHost");
                return;
            }

            var ret = OpCodes.Ret.ToInstruction();
            var patchedMethod = new[]
            {
                // if (Process.GetProcessesByName("IstaServicesHost").FirstOrDefault() == null)
                OpCodes.Ldstr.ToInstruction("IstaServicesHost"),
                OpCodes.Call.ToInstruction(getProcessesByName),
                OpCodes.Call.ToInstruction(firstOrDefault),
                OpCodes.Brtrue_S.ToInstruction(ret),

                // throw new InvalidOperationException("Host not found.");
                OpCodes.Ldstr.ToInstruction("Host not found."),
                OpCodes.Newobj.ToInstruction(invalidOperationException),
                OpCodes.Throw.ToInstruction(),

                ret,
            };

            method.ReplaceWith(patchedMethod);
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
        }
    }

    [EssentialPatch]
    public static int PatchIstaProcessStarter(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
            "CheckSignature",
            "(System.String)System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchPackageValidityService(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.ISTAGUI.Controller.PackageValidityService",
            "CyclicExpirationDateCheck",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchServiceProgramCompilerLicense(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.ExternalToolLicense.ServiceProgramCompilerLicense",
            "CheckLicenseExpiration",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EssentialPatch]
    public static int PatchConfigurationService(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.Psdz.Services.ConfigurationService",
            "SetPsdzProperties",
            "(System.String,System.String,System.String,System.String)System.Void",
            RewriteProperties
        );

        void RewriteProperties(MethodDef method)
        {
            var getBaseService = method.FindOperand<MemberRef>(OpCodes.Call, "com.bmw.psdz.api.Configuration BMW.Rheingold.Psdz.Services.ServiceBase`1<com.bmw.psdz.api.Configuration>::get_BaseService()");
            var getPSdZProperties = method.FindOperand<MemberRef>(OpCodes.Callvirt, "java.util.Properties com.bmw.psdz.api.Configuration::getPSdZProperties()");
            var setPSdZProperties = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void com.bmw.psdz.api.Configuration::setPSdZProperties(java.util.Properties)");
            var putProperty = method.FindOperand<MethodDef>(OpCodes.Call, "System.Void BMW.Rheingold.Psdz.Services.ConfigurationService::PutProperty(java.util.Properties,java.lang.String,java.lang.String)");
            var stringImplicit = method.FindOperand<MemberRef>(OpCodes.Call, "java.lang.String java.lang.String::op_Implicit(System.String)");

            if (getBaseService == null || getPSdZProperties == null || setPSdZProperties == null || putProperty == null ||
                stringImplicit == null)
            {
                Log.Warning("Required instructions not found, can not patch ConfigurationService::SetPsdzProperties");
                return;
            }

            if (method.Body.Variables.Count != 1)
            {
                var property = method.GetLocalByType("java.util.Properties");
                if (property == null || method.Body.Variables.Count == 0)
                {
                    Log.Warning("Properties not found, can not patch ConfigurationService::SetPsdzProperties");
                    return;
                }

                method.Body.Variables.Clear();
                method.Body.Variables.Add(property);
            }

            var patchedMethod = new[]
            {
                // Properties pSdZProperties = base.BaseService.getPSdZProperties();
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService),
                OpCodes.Callvirt.ToInstruction(getPSdZProperties),
                OpCodes.Stloc_0.ToInstruction(),

                // PutProperty(pSdZProperties, String.op_Implicit("DealerID"), String.op_Implicit("1234"));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("DealerID"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldstr.ToInstruction("1234"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // PutProperty(pSdZProperties, String.op_Implicit("PlantID"), String.op_Implicit("0"));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("PlantID"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldstr.ToInstruction("0"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // PutProperty(pSdZProperties, String.op_Implicit("ProgrammierGeraeteSeriennummer"), String.op_Implicit(programmierGeraeteSeriennummer));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("ProgrammierGeraeteSeriennummer"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldarg_3.ToInstruction(),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // PutProperty(pSdZProperties, String.op_Implicit("Testereinsatzkennung"), String.op_Implicit(testerEinsatzKennung));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("Testereinsatzkennung"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldarg_S.ToInstruction(method.Parameters[4]),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // base.BaseService.setPSdZProperties(pSdZProperties);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setPSdZProperties),
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
        }
    }

    [EssentialPatch]
    public static int PatchInteractionModel(ModuleDefMD module)
    {
        return module.PatcherGetter(
            "BMW.Rheingold.CoreFramework.Interaction.Models.InteractionModel",
            "Title",
            RewriteTitle
        );

        void RewriteTitle(MethodDef method)
        {
            var stringRef = module.CorLibTypes.String.TypeRef;
            var concatRef = new MemberRefUser(
                module,
                "Concat",
                MethodSig.CreateStatic(module.CorLibTypes.String, module.CorLibTypes.String, module.CorLibTypes.String),
                stringRef);

            var containsDef = module.Types.SelectMany(t => t.Methods).Where(m => m.HasBody)
                                    .SelectMany(m => m.Body.Instructions)
                                    .FirstOrDefault(i => i.OpCode == OpCodes.Callvirt &&
                                                         (i.Operand as MemberRef)?.FullName ==
                                                         "System.Boolean System.String::Contains(System.String)")
                                    ?.Operand as MemberRef;

            var titleField = method.DeclaringType.Fields.FirstOrDefault(field =>
                field.FullName ==
                "System.String BMW.Rheingold.CoreFramework.Interaction.Models.InteractionModel::title");

            if (containsDef == null || titleField == null)
            {
                Log.Warning("Required instructions not found, can not patch InteractionModel::Title");
                return;
            }

            var label = OpCodes.Nop.ToInstruction();
            var patchedMethod = new[]
            {
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldfld.ToInstruction(titleField),
                OpCodes.Brfalse_S.ToInstruction(label),

                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldfld.ToInstruction(titleField),
                OpCodes.Ldstr.ToInstruction("ISTA-Patcher"),
                OpCodes.Callvirt.ToInstruction(containsDef),
                OpCodes.Brfalse_S.ToInstruction(label),

                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldfld.ToInstruction(titleField),
                OpCodes.Ret.ToInstruction(),

                label,
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldfld.ToInstruction(titleField),
                OpCodes.Ldstr.ToInstruction($" ({PoweredBy})"),
                OpCodes.Call.ToInstruction(concatRef),
                OpCodes.Ret.ToInstruction(),
            };
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.ReplaceWith(patchedMethod);
        }
    }

    [EssentialPatch]
    public static int PatchRuntimeEnvironment(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.RuntimeEnvironment",
            "GetSubVersion",
            "(System.UInt32 modopt(System.Runtime.CompilerServices.IsLong),System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&,System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&,System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&,System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&)System.Void",
            RemoveHypervisorFlag
        );

        void RemoveHypervisorFlag(MethodDef method)
        {
            var instructions = method.Body.Instructions;
            instructions.RemoveAt(instructions.Count - 1);

            var appendInstructions = new[]
            {
                // ecx = ecx & 0x7fffffff
                OpCodes.Ldarg_3.ToInstruction(),
                OpCodes.Ldarg_3.ToInstruction(),
                OpCodes.Ldind_U4.ToInstruction(),
                OpCodes.Ldc_I4.ToInstruction(0x7fffffff),
                OpCodes.And.ToInstruction(),
                OpCodes.Stind_I4.ToInstruction(),

                // return;
                OpCodes.Ret.ToInstruction(),
            };

            foreach (var instruction in appendInstructions)
            {
                instructions.Add(instruction);
            }
        }
    }

    [SignaturePatch]
    public static Func<ModuleDefMD, int> PatchGetRSAPKCS1SignatureDeformatter(string modulus, string exponent)
    {
        return module =>
        {
            return module.PatchFunction(
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
                "GetRSAPKCS1SignatureDeformatter",
                "()System.Security.Cryptography.RSAPKCS1SignatureDeformatter",
                ReplaceParameters
            );

            void ReplaceParameters(MethodDef method)
            {
                var ldStrInstructions = method.Body.Instructions.Where(inst => inst.OpCode == OpCodes.Ldstr).ToList();
                if (ldStrInstructions.Count == 3)
                {
                    ldStrInstructions[0].Operand = modulus;
                    ldStrInstructions[1].Operand = exponent;
                }
                else
                {
                    Log.Warning("instruction ldstr count not match, can not patch LicenseStatusChecker");
                }
            }
        };
    }
}
