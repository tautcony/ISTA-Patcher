// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Core;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ISTAlter.Utils;
using Serilog;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the optional part of the patching logic.
/// </summary>
public static partial class PatchUtils
{
    [FinishedOPPatch]
    public static int PatchIsProgrammingSession(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.IstaServices.Contract.PUK.Data.TransactionMetaData",
            "get_IsProgrammingSession",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
         ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.OperationFinishedListViewModel",
            "PerformAcceptOperation",
            "(\u0042\u004d\u0057.ISPI.IstaServices.Contract.PUK.Data.TransactionMetaData)System.Void",
            RemoveIsProgrammingEnabledCheck
        );

        void RemoveIsProgrammingEnabledCheck(MethodDef def)
        {
            const string patchTargetName = "OperationFinishedListViewModel::PerformAcceptOperation";

            if (def.CustomAttributes.FirstOrDefault() is not { ConstructorArguments: [{ Value: ValueTypeSig stateMachineType }] })
            {
                Log.Warning($"Attribute not found, can not patch {patchTargetName}");
                return;
            }

            var typeDef = stateMachineType.TypeDefOrRef.ResolveTypeDef();
            if (typeDef?.Methods.FirstOrDefault(m => m.Name == "MoveNext" && m.HasOverrides) is not { } method)
            {
                Log.Warning($"Method not found, can not patch {patchTargetName}");
                return;
            }

            if (method.Body.Instructions.FirstOrDefault(inst =>
                    inst.OpCode == OpCodes.Call && inst.Operand is IMethod methodOperand &&
                    methodOperand.Name == "IsProgrammingEnabled") is not { } instruction)
            {
                Log.Warning($"Required instruction not found, can not patch {patchTargetName}");
                return;
            }

            instruction.OpCode = OpCodes.Ldc_I4_1;
            instruction.Operand = null;
        }
    }

    [ENETPatch]
    public static int PatchTherapyPlanCalculated(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Programming.States.TherapyPlanCalculated",
            "IsConnectedViaENETAndBrandIsToyota",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
         );
    }

    [RequirementsPatch]
    public static int PatchIstaInstallationRequirements(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.IstaInstallationRequirements",
            "CheckSystemRequirements",
            "(System.Boolean)System.Collections.Generic.Dictionary`2<\u0042\u004d\u0057.Rheingold.ISTAGUI._new.ViewModels.InsufficientSystemRequirement,System.Int32[]>",
            RemoveRequirementsCheck
        );

        void RemoveRequirementsCheck(MethodDef method)
        {
            var dictionaryCtorRef = method.FindOperand<MemberRef>(
                OpCodes.Newobj,
                "System.Void System.Collections.Generic.Dictionary`2<\u0042\u004d\u0057.Rheingold.ISTAGUI._new.ViewModels.InsufficientSystemRequirement,System.Int32[]>::.ctor()");

            if (dictionaryCtorRef == null)
            {
                Log.Warning("Required instructions not found, can not patch IstaInstallationRequirements::CheckSystemRequirements");
                return;
            }

            DnlibUtils.ReturnObjectMethod(dictionaryCtorRef)(method);
        }
    }

    [RequirementsPatch]
    public static int PatchRuntimeEnvironment(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.RuntimeEnvironment",
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

    [NotSendPatch]
    public static int PatchMultisessionLogic(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.MultisessionLogic",
            "SetIsSendFastaDataForbidden",
            "()System.Void",
            SetNotSendFastData
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.MultisessionLogic",
            "SetIsSendOBFCMDataForbidden",
            "()System.Void",
            SetNotSendOBFCMData
        );

        void SetNotSendOBFCMData(MethodDef method)
        {
            var get_CurrentOperation = method.FindOperand<MethodDef>(OpCodes.Call, "\u0042\u004d\u0057.Rheingold.PresentationFramework.Contracts.IIstaOperation \u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.MultisessionLogic::get_CurrentOperation()");
            var setIsSendOBFCMDataIsForbidden = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void \u0042\u004d\u0057.ISPI.IstaOperation.Contract.IIstaOperationService::SetIsSendOBFCMDataIsForbidden(System.Boolean)");
            var onPropertyChanged = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void \u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic::OnPropertyChanged(System.String)");

            if (get_CurrentOperation == null || setIsSendOBFCMDataIsForbidden == null || onPropertyChanged == null)
            {
                Log.Warning("Required instructions not found, can not patch MultisessionLogic::SetNotSendOBFCMData");
                return;
            }

            var patchedMethod = new[]
            {
                // this.CurrentOperation.SetIsSendOBFCMDataIsForbidden(true);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(get_CurrentOperation),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setIsSendOBFCMDataIsForbidden),

                // this.OnPropertyChanged("isSendOBFCMDataForbidden");
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("isSendOBFCMDataForbidden"),
                OpCodes.Callvirt.ToInstruction(onPropertyChanged),

                // return;
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
        }

        void SetNotSendFastData(MethodDef method)
        {
            var get_CurrentOperation = method.FindOperand<MethodDef>(OpCodes.Call, "\u0042\u004d\u0057.Rheingold.PresentationFramework.Contracts.IIstaOperation \u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.MultisessionLogic::get_CurrentOperation()");
            var get_DataContext = method.FindOperand<MemberRef>(OpCodes.Callvirt, "\u0042\u004d\u0057.ISPI.IstaOperation.Contract.IIstaOperationDataContext \u0042\u004d\u0057.Rheingold.PresentationFramework.Contracts.IIstaOperation::get_DataContext()");
            var get_VecInfo = method.FindOperand<MemberRef>(OpCodes.Callvirt, "\u0042\u004d\u0057.Rheingold.CoreFramework.Contracts.Vehicle.IVehicle \u0042\u004d\u0057.ISPI.IstaOperation.Contract.IIstaOperationDataContext::get_VecInfo()");
            var set_IsSendFastaDataForbidden = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void \u0042\u004d\u0057.Rheingold.CoreFramework.Contracts.Vehicle.IVehicle::set_IsSendFastaDataForbidden(System.Boolean)");
            var setIsSendFastaDataIsForbidden = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void \u0042\u004d\u0057.ISPI.IstaOperation.Contract.IIstaOperationService::SetIsSendFastaDataIsForbidden(System.Boolean)");
            var onPropertyChanged = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void \u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic::OnPropertyChanged(System.String)");

            if (get_CurrentOperation == null || get_DataContext == null || get_VecInfo == null || set_IsSendFastaDataForbidden == null || setIsSendFastaDataIsForbidden == null || onPropertyChanged == null)
            {
                Log.Warning("Required instructions not found, can not patch MultisessionLogic::SetNotSendFastData");
                return;
            }

            var patchedMethod = new[]
            {
                // this.CurrentOperation.DataContext.VecInfo.IsSendFastaDataForbidden = true;
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(get_CurrentOperation),
                OpCodes.Callvirt.ToInstruction(get_DataContext),
                OpCodes.Callvirt.ToInstruction(get_VecInfo),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(set_IsSendFastaDataForbidden),

                // this.CurrentOperation.SetIsSendFastaDataIsForbidden(true);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(get_CurrentOperation),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setIsSendFastaDataIsForbidden),

                // this.OnPropertyChanged("IsSendFastaDataForbidden");
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("IsSendFastaDataForbidden"),
                OpCodes.Callvirt.ToInstruction(onPropertyChanged),

                // return;
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
        }
    }

    [MarketLanguagePatch]
    public static Func<ModuleDefMD, int> PatchCommonServiceWrapper_GetMarketLanguage(string marketLanguage)
    {
        return module => module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
            "GetMarketLanguage",
            "()System.String",
            DnlibUtils.ReturnStringMethod(marketLanguage)
        );
    }

    [EnableOfflinePatch]
    public static int PatchConfigSettings(ModuleDefMD module)
    {
        return module.PatcherGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "IsILeanActive",
            DnlibUtils.ReturnFalseMethod
        ) + module.PatcherGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "IsOssModeActive",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [UserAuthPatch]
    [FromVersion("4.44.1x")]
    public static int PatchUserEnvironmentProvider(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.PresentationFramework.Authentication.UserEnvironmentProvider",
            "GetCurrentUserEnvironment",
            "()\u0042\u004d\u0057.Rheingold.PresentationFramework.Authentication.UserEnvironment",
            DnlibUtils.ReturnUInt32Method(2) // PROD
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.PresentationFramework.Authentication.UserEnvironmentProvider",
            "GetCurrentNetworkType",
            "()\u0042\u004d\u0057.Rheingold.PresentationFramework.Authentication.NetworkType",
            DnlibUtils.ReturnUInt32Method(1) // LAN
        );
    }

    [UserAuthPatch]
    [FromVersion("4.48.x")]
    public static int PatchLoginOptionsProvider(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.PresentationFramework.AuthenticationRefactored.Services.LoginOptionsProvider",
            "IsLoginEnabled",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [SyncClientConfig]
    [LibraryName("CommonServices")]
    [FromVersion("4.46.3x")]
    public static int PatchClientConfigurationManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.iLean.CommonServices.Services.ClientConfigurationManager",
            "CheckClientConfigurationChangedDate",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [DisableFakeFSCRejectPatch]
    public static int PatchRetrieveActualSwtInfoState(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.RetrieveActualSwtInfoState",
            "Handle",
            "(\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.ProgrammingSession)\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.DefaultStateResult",
            SetPeriodicalCheck
        );

        void SetPeriodicalCheck(MethodDef method)
        {
            var instructions = method.Body.Instructions;
            var requestSwtAction = method.FindInstruction(OpCodes.Callvirt, "\u0042\u004d\u0057.Rheingold.Psdz.Model.Swt.IPsdzSwtAction \u0042\u004d\u0057.Rheingold.Psdz.IProgrammingService::RequestSwtAction(\u0042\u004d\u0057.Rheingold.Psdz.Model.IPsdzConnection,System.Boolean)");
            if (requestSwtAction == null)
            {
                Log.Warning("Required instructions not found, can not patch RetrieveActualSwtInfoState::Handle");
                return;
            }

            var indexOfRequestSwtAction = instructions.IndexOf(requestSwtAction);
            var ldcI4One = instructions[indexOfRequestSwtAction - 1];
            if (!ldcI4One.IsLdcI4())
            {
                Log.Warning("Required instructions not found, can not patch RetrieveActualSwtInfoState::Handle");
                return;
            }

            ldcI4One.OpCode = OpCodes.Ldc_I4_0;
        }
    }

    [DisableFakeFSCRejectPatch]
    public static int PatchRetrieveActualSwtEnablingCodesState(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.RetrieveActualSwtEnablingCodesState",
            "Handle",
            "(\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.ProgrammingSession)\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.DefaultStateResult",
            SetPeriodicalCheck
        );

        void SetPeriodicalCheck(MethodDef method)
        {
            var instructions = method.Body.Instructions;
            var requestSwtAction = method.FindInstruction(OpCodes.Callvirt, "\u0042\u004d\u0057.Rheingold.Psdz.Model.Swt.IPsdzSwtAction \u0042\u004d\u0057.Rheingold.Psdz.IProgrammingService::RequestSwtAction(\u0042\u004d\u0057.Rheingold.Psdz.Model.IPsdzConnection,System.Boolean)");
            if (requestSwtAction == null)
            {
                Log.Warning("Required instructions not found, can not patch RetrieveActualSwtEnablingCodesState::Handle");
                return;
            }

            var indexOfRequestSwtAction = instructions.IndexOf(requestSwtAction);
            var ldcI4One = instructions[indexOfRequestSwtAction - 1];
            if (!ldcI4One.IsLdcI4())
            {
                Log.Warning("Required instructions not found, can not patch RetrieveActualSwtEnablingCodesState::Handle");
                return;
            }

            ldcI4One.OpCode = OpCodes.Ldc_I4_0;
        }
    }

    [EnableAirClientPatch]
    public static int PatchMainWindowIconBarViewModel(ModuleDefMD module)
    {
        return module.PatcherGetter(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.MainWindowIconBarViewModel",
            "IsAirActive",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EnableAirClientPatch]
    public static int PatchAirForkServicesWrapper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldISPINext.AIR.AirForkServicesWrapper",
            "GetAirLauncher",
            "(\u0042\u004d\u0057.ISPI.IstaServices.Contract.ICS.IIstaIcsService)\u0042\u004d\u0057.ISPI.AIR.AIRClient.AirForkServices.IAirLauncher",
            ReplaceCondition);

        void ReplaceCondition(MethodDef method)
        {
            var callIsILeanActive = method.FindInstruction(OpCodes.Call, "System.Boolean \u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings::get_IsILeanActive()");
            if (callIsILeanActive == null)
            {
                Log.Warning("Required instructions not found, can not patch AirForkServicesWrapper::GetAirLauncher");
                return;
            }

            var instructions = method.Body.Instructions;
            var indexOfCallIsILeanActive = instructions.IndexOf(callIsILeanActive);
            var instruction = instructions[indexOfCallIsILeanActive];
            instruction.OpCode = OpCodes.Ldc_I4_1;
            instruction.Operand = null;
        }
    }
}
