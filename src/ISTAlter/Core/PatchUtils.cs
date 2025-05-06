// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

namespace ISTAlter.Core;

using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ISTAlter.Utils;
using Serilog;
using ZLinq;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the main part of the patching logic.
/// </summary>
public static partial class PatchUtils
{
    [ValidationPatch]
    public static int PatchLicenseStatusChecker(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
            "IsLicenseValid",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseInfo,System.Boolean)\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseStatus",
            DnlibUtils.ReturnZeroMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseWizardHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
            "DoLicenseCheck",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManager",
            "VerifyLicense",
            "(System.Boolean)System.Void",
            DnlibUtils.EmptyingMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManager",
            "CheckRITALicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManager",
            "LastCompileTimeIsInvalid",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManager",
            "LastCompileTimeServiceProgramCompilerLicenseIsValid",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseHelper",
            "IsVehicleLockedDown",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.Vehicle)System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [ValidationPatch]
    [LibraryName("RheingoldISPINext.dll")]
    public static int PatchCommonServiceWrapper_VerifyLicense(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
            "VerifyLicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [ValidationPatch]
    [LibraryName("CommonServices.dll")]
    public static int PatchSecureAccessHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.iLean.CommonServices.Helper.SecureAccessHelper",
            "IsCodeAccessPermitted",
            "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    [LibraryName("ISTAGUI.exe")]
    public static int PatchMainWindowViewModel(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
            "CheckExpirationDate",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [ValidationPatch]
    [LibraryName("CommonServices.dll")]
    public static int PatchActivationCertificateHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsInWhiteList",
            "(System.String,System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsWhiteListSignatureValid",
            "(System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    [LibraryName("RheingoldExternalToolLicense.dll")]
    public static int PatchServiceProgramCompilerLicense(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ExternalToolLicense.ServiceProgramCompilerLicense",
            "CheckLicenseExpiration",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    [LibraryName("PsdzServiceImpl.dll")]
    public static int PatchConfigurationService(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Psdz.Services.ConfigurationService",
            "SetPsdzProperties",
            "(System.String,System.String,System.String,System.String)System.Void",
            RewriteProperties
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Psdz.Services.ConfigurationService",
            "SetPsdzProperties",
            "(System.String,System.String,System.String,System.String,System.String)System.Void",
            RewriteProperties
        );

        void RewriteProperties(MethodDef method)
        {
            var getBaseService = method.FindOperand<MemberRef>(OpCodes.Call, "com.\u0062\u006d\u0077.psdz.api.Configuration \u0042\u004d\u0057.Rheingold.Psdz.Services.ServiceBase`1<com.\u0062\u006d\u0077.psdz.api.Configuration>::get_BaseService()");
            var getPSdZProperties = method.FindOperand<MemberRef>(OpCodes.Callvirt, "java.util.Properties com.\u0062\u006d\u0077.psdz.api.Configuration::getPSdZProperties()");
            var setPSdZProperties = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void com.\u0062\u006d\u0077.psdz.api.Configuration::setPSdZProperties(java.util.Properties)");
            var putProperty = method.FindOperand<MethodDef>(OpCodes.Call, "System.Void \u0042\u004d\u0057.Rheingold.Psdz.Services.ConfigurationService::PutProperty(java.util.Properties,java.lang.String,java.lang.String)");
            var stringImplicit = method.FindOperand<MemberRef>(OpCodes.Call, "java.lang.String java.lang.String::op_Implicit(System.String)");

            if (getBaseService == null || getPSdZProperties == null || setPSdZProperties == null || putProperty == null ||
                stringImplicit == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            if (method.Body.Variables.Count != 1)
            {
                var property = method.GetLocalByType("java.util.Properties");
                if (property == null || method.Body.Variables.Count == 0)
                {
                    Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                    return;
                }

                method.Body.Variables.Clear();
                method.Body.Variables.Add(property);
            }

            var patchedMethod = new List<Instruction>
            {
                // Properties pSdZProperties = base.BaseService.getPSdZProperties();
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService),
                OpCodes.Callvirt.ToInstruction(getPSdZProperties),
                OpCodes.Stloc_0.ToInstruction(),
            };

            // PutProperty(pSdZProperties, String.op_Implicit("DealerID"), String.op_Implicit("1234"));
            patchedMethod.AddRange(PutProperty("DealerID", OpCodes.Ldstr.ToInstruction("1234")));

            // PutProperty(pSdZProperties, String.op_Implicit("PlantID"), String.op_Implicit("0"));
            patchedMethod.AddRange(PutProperty("PlantID", OpCodes.Ldstr.ToInstruction("0")));

            // PutProperty(pSdZProperties, String.op_Implicit("ProgrammierGeraeteSeriennummer"), String.op_Implicit(programmierGeraeteSeriennummer));
            patchedMethod.AddRange(PutProperty("ProgrammierGeraeteSeriennummer", OpCodes.Ldarg_3.ToInstruction()));

            // PutProperty(pSdZProperties, String.op_Implicit("Testereinsatzkennung"), String.op_Implicit(testerEinsatzKennung));
            patchedMethod.AddRange(PutProperty("Testereinsatzkennung", OpCodes.Ldarg_S.ToInstruction(method.Parameters[4])));

            if (method.Parameters.Count == 6)
            {
                // PutProperty(psdZProperties, "ProgrammingSequenceUseCase", programmingSequenceUseCase);
                patchedMethod.AddRange(PutProperty("ProgrammingSequenceUseCase", OpCodes.Ldarg_S.ToInstruction(method.Parameters[5])));
            }

            // base.BaseService.setPSdZProperties(pSdZProperties);
            patchedMethod.AddRange([
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setPSdZProperties),
                OpCodes.Ret.ToInstruction(),
            ]);

            method.ReplaceWith(patchedMethod);
            return;

            Instruction[] PutProperty(string propertyName, Instruction valueInstruction)
            {
                return
                [
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldloc_0.ToInstruction(),
                    OpCodes.Ldstr.ToInstruction(propertyName),
                    OpCodes.Call.ToInstruction(stringImplicit),
                    valueInstruction,
                    OpCodes.Call.ToInstruction(stringImplicit),
                    OpCodes.Call.ToInstruction(putProperty),
                ];
            }
        }
    }

    [EssentialPatch]
    public static int PatchIntegrityManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.SecurityAndLicense.IntegrityManager",
            ".ctor",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchVerifyAssemblyHelper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
            "VerifyStrongName",
            "(System.String,System.Boolean)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.Interaction.Models.InteractionModel",
            "Title",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    [LibraryName("IstaServicesClient.dll")]
    public static int PatchIstaIcsServiceClient(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.IstaServices.Client.IstaIcsServiceClient",
            "ValidateHost",
            "()System.Void",
            RemovePublicKeyCheck
        );

        void RemovePublicKeyCheck(MethodDef method)
        {
            var getProcessesByName = method.FindOperand<MemberRef>(OpCodes.Call, "System.Diagnostics.Process[] System.Diagnostics.Process::GetProcessesByName(System.String)");
            var firstOrDefault = method.FindOperand<MethodSpec>(OpCodes.Call, "System.Diagnostics.Process System.Linq.Enumerable::FirstOrDefault<System.Diagnostics.Process>(System.Collections.Generic.IEnumerable`1<System.Diagnostics.Process>)");
            var invalidOperationException = method.FindOperand<MemberRef>(OpCodes.Newobj, "System.Void System.InvalidOperationException::.ctor(System.String)");
            if (getProcessesByName == null || firstOrDefault == null || invalidOperationException == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
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
    [LibraryName("RheingoldCoreFramework.dll")]
    public static int PatchIstaProcessStarter(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
            "CheckSignature",
            "(System.String)System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchPackageValidityService(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.PackageValidityService",
            "CyclicExpirationDateCheck",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    public static int PatchInteractionModel(ModuleDefMD module)
    {
        return module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.Interaction.Models.InteractionModel",
            "Title",
            method =>
            {
                var containsDef = module.Types
                    .AsValueEnumerable()
                    .SelectMany(t => t.Methods)
                    .Where(m => m.HasBody)
                    .SelectMany(m => m.Body.Instructions)
                    .FirstOrDefault(i => i.OpCode == OpCodes.Callvirt && string.Equals((i.Operand as MemberRef)?.FullName, "System.Boolean System.String::Contains(System.String)", StringComparison.Ordinal))?.Operand as MemberRef;
                var titleField = method.DeclaringType.Fields
                    .AsValueEnumerable()
                    .FirstOrDefault(field => string.Equals(field.FullName, "System.String \u0042\u004d\u0057.Rheingold.CoreFramework.Interaction.Models.InteractionModel::title", StringComparison.Ordinal));

                if (containsDef == null || titleField == null)
                {
                    return;
                }

                var stringRef = module.CorLibTypes.String.TypeRef;
                var concatRef = new MemberRefUser(module,  "Concat", MethodSig.CreateStatic(module.CorLibTypes.String, module.CorLibTypes.String, module.CorLibTypes.String), stringRef);

                var label = OpCodes.Nop.ToInstruction();
                var patchedMethod = new[]
                {
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldfld.ToInstruction(titleField),
                    OpCodes.Brfalse_S.ToInstruction(label),

                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldfld.ToInstruction(titleField),
                    OpCodes.Ldstr.ToInstruction(GetCoefficients().GetString(12)),
                    OpCodes.Callvirt.ToInstruction(containsDef),
                    OpCodes.Brfalse_S.ToInstruction(label),

                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldfld.ToInstruction(titleField),
                    OpCodes.Ret.ToInstruction(),

                    label,
                    OpCodes.Ldarg_0.ToInstruction(),
                    OpCodes.Ldfld.ToInstruction(titleField),
                    OpCodes.Ldstr.ToInstruction($" ({Encoding.UTF8.GetString(Config)})"),
                    OpCodes.Call.ToInstruction(concatRef),
                    OpCodes.Ret.ToInstruction(),
                };
                method.Body.Variables.Clear();
                method.Body.ExceptionHandlers.Clear();
                method.ReplaceWith(patchedMethod);
            }
        );
    }

    [SignaturePatch]
    public static Func<ModuleDefMD, int> PatchGetRSAPKCS1SignatureDeformatter(string modulus, string exponent)
    {
        return module =>
        {
            return module.PatchFunction(
                "\u0042\u004d\u0057.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
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
                    Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                }
            }
        };
    }
}
