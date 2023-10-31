// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.Core;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Serilog;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the optional part of the patching logic.
/// </summary>
internal static partial class PatchUtils
{
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
}
