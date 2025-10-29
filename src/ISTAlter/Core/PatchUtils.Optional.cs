// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

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
    [LibraryName("IstaServicesContract.dll")]
    public static int PatchIsProgrammingSession(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.IstaServices.Contract.PUK.Data.TransactionMetaData",
            "get_IsProgrammingSession",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [FinishedOPPatch]
    [LibraryName("ISTAGUI.exe")]
    public static int PatchOperationFinishedListViewModel(ModuleDefMD module)
    {
        return module.PatchAsyncFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.OperationFinishedListViewModel",
            "PerformAcceptOperation",
            "(\u0042\u004d\u0057.ISPI.IstaServices.Contract.PUK.Data.TransactionMetaData)System.Void",
            RemoveIsProgrammingEnabledCheck
        );

        static void RemoveIsProgrammingEnabledCheck(MethodDef method)
        {
            if (method.Body.Instructions.FirstOrDefault(inst =>
                    inst.OpCode == OpCodes.Call && inst.Operand is IMethod methodOperand &&
                    methodOperand.Name == "IsProgrammingEnabled") is not { } instruction)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            instruction.OpCode = OpCodes.Ldc_I4_1;
            instruction.Operand = null;
        }
    }

    [ENETPatch]
    [LibraryName("RheingoldProgramming.dll")]
    public static int PatchTherapyPlanCalculated(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Programming.States.TherapyPlanCalculated",
            "IsConnectedViaENETAndBrandIsToyota",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
         );
    }

    [ENETPatch]
    [LibraryName("RheingoldVehicleCommunication.dll")]
    public static int PatchInitializeEnetDevice(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.VehicleCommunication.ECUKom",
            "InitializeEnetDevice",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.Contracts.Vehicle.IVciDevice)System.Boolean",
            ModifyEnetInitialization
        );

        static void ModifyEnetInitialization(MethodDef method)
        {
            var apiInitExtCalls = method.FindInstructions(OpCodes.Callvirt, "System.Boolean \u0042\u004d\u0057.Rheingold.VehicleCommunication.EdiabasToolbox.API::apiInitExt(System.String,System.String,System.String,System.String)");

            if (apiInitExtCalls.Count < 2)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Find the second apiInitExt call (the one that returns empty string)
            var secondApiInitExt = apiInitExtCalls[1];
            var indexOfSecondCall = method.Body.Instructions.IndexOf(secondApiInitExt);

            if (indexOfSecondCall is -1 or < 1)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Replace the empty string parameter with the new connection string
            // The parameter should be just before the call: ldstr ""
            var emptyStringInstruction = method.Body.Instructions[indexOfSecondCall - 1];
            if (emptyStringInstruction.OpCode != OpCodes.Ldstr || !string.IsNullOrEmpty(emptyStringInstruction.Operand as string))
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Find the device parameter load to construct the new string
            var get_IPAddress = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.String \u0042\u004d\u0057.Rheingold.CoreFramework.Contracts.Vehicle.IVciDevice::get_IPAddress()");
            if (get_IPAddress == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Replace the empty string load with construction of the new connection string
            var newInstructions = new[]
            {
                OpCodes.Ldstr.ToInstruction("RemoteHost="),
                OpCodes.Ldarg_1.ToInstruction(), // device parameter
                OpCodes.Callvirt.ToInstruction(get_IPAddress),
                OpCodes.Ldstr.ToInstruction(";DiagnosticPort=6801;ControlPort=6811"),
                OpCodes.Call.ToInstruction(method.Module.Import(typeof(string).GetMethod("Concat", [typeof(string), typeof(string), typeof(string)]))),
            };

            // Remove the original empty string instruction
            method.Body.Instructions.RemoveAt(indexOfSecondCall - 1);

            // Insert the new instructions
            for (var i = newInstructions.Length - 1; i >= 0; i--)
            {
                method.Body.Instructions.Insert(indexOfSecondCall - 1, newInstructions[i]);
            }
        }
    }

    [RequirementsPatch]
    [LibraryName("ISTAGUI.exe")]
    [UntilVersion("4.52")]
    public static int PatchIstaInstallationRequirements(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.IstaInstallationRequirements",
            "CheckSystemRequirements",
            "(System.Boolean)System.Collections.Generic.Dictionary`2<\u0042\u004d\u0057.Rheingold.ISTAGUI._new.ViewModels.InsufficientSystemRequirement,System.Int32[]>",
            RemoveRequirementsCheck
        );

        static void RemoveRequirementsCheck(MethodDef method)
        {
            var dictionaryCtorRef = method.FindOperand<MemberRef>(
                OpCodes.Newobj,
                "System.Void System.Collections.Generic.Dictionary`2<\u0042\u004d\u0057.Rheingold.ISTAGUI._new.ViewModels.InsufficientSystemRequirement,System.Int32[]>::.ctor()");

            if (dictionaryCtorRef == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            DnlibUtils.ReturnObjectMethod(dictionaryCtorRef)(method);
        }
    }

    [RequirementsPatch]
    [LibraryName("RheingoldCoreBootstrap.dll")]
    [UntilVersion("4.46")]
    public static int PatchRuntimeEnvironment(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.RuntimeEnvironment",
            "GetSubVersion",
            "(System.UInt32 modopt(System.Runtime.CompilerServices.IsLong),System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&,System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&,System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&,System.UInt32 modopt(System.Runtime.CompilerServices.IsLong)&)System.Void",
            RemoveHypervisorFlag
        );

        static void RemoveHypervisorFlag(MethodDef method)
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
    [LibraryName("ISTAGUI.exe")]
    [UntilVersion("4.46")]
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
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
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
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
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

    [NotSendPatch]
    [LibraryName("ISTAGUI.exe")]
    [FromVersion("4.55")]
    public static int PatchMultisessionLogicFrom455(ModuleDefMD module)
    {
        return module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.MultisessionLogic",
            "IsSendFastaDataForbidden",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.Controller.MultisessionLogic",
            "IsSendOBFCMDataForbidden",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [NotSendPatch]
    [LibraryName("ISTAGUI.exe")]
    [FromVersion("4.55")]
    public static int PatchSendFileToFbmThroughMultisessionLogic(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.OperationFinishedListViewModel",
            "SendFileToFbmThroughMultisessionLogic",
            "(\u0042\u004d\u0057.ISPI.IstaServices.Contract.PUK.Data.TransactionMetaData,System.String,System.IO.FileInfo)System.Void",
            ChangeSendFastaDataToFBMParameter
        );

        static void ChangeSendFastaDataToFBMParameter(MethodDef method)
        {
            var sendFastaDataToFBMCall = method.FindInstruction(OpCodes.Callvirt, "System.String \u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic::SendFastaDataToFBM(System.String,System.Boolean)");
            if (sendFastaDataToFBMCall == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            var indexOfCall = method.Body.Instructions.IndexOf(sendFastaDataToFBMCall);
            if (indexOfCall == -1 || indexOfCall < 1)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // The boolean parameter should be just before the call
            var booleanInstruction = method.Body.Instructions[indexOfCall - 1];
            if (!booleanInstruction.IsLdcI4())
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Change true (ldc.i4.1) to false (ldc.i4.0)
            booleanInstruction.OpCode = OpCodes.Ldc_I4_0;
        }
    }

    [NotSendPatch]
    [LibraryName("RheingoldCoreFramework.dll")]
    [FromVersion("4.55")]
    public static int PatchtypeVehicle(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.typeVehicle",
            "get_IsSendFastaDataForbidden",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.typeVehicle",
            "set_IsSendFastaDataForbidden",
            "(System.Boolean)System.Void",
            method =>
            {
                method.ReplaceWith([OpCodes.Ret.ToInstruction()]);
                method.Body.Variables.Clear();
                method.Body.ExceptionHandlers.Clear();
            }
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.typeVehicle",
            "get_IsSendOBFCMDataForbidden",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.typeVehicle",
            "set_IsSendOBFCMDataForbidden",
            "(System.Boolean)System.Void",
            method =>
            {
                method.ReplaceWith([OpCodes.Ret.ToInstruction()]);
                method.Body.Variables.Clear();
                method.Body.ExceptionHandlers.Clear();
            }
        );
    }

    [NotSendPatch]
    [LibraryName("RheingoldSessionController.dll")]
    [FromVersion("4.55")]
    public static int PatchLogic(ModuleDefMD module)
    {
        return module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic",
            "IsSendFastaDataForbidden",
            DnlibUtils.ReturnTrueMethod
        ) + module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic",
            "IsSendOBFCMDataForbidden",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [NotSendPatch]
    [LibraryName("RheingoldSessionController.dll")]
    [FromVersion("4.55")]
    public static int PatchFASTATransferMode(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Core.GlobalSettingsObject",
            "get_FASTATransferMode",
            "()\u0042\u004d\u0057.Rheingold.CoreFramework.EnumFASTATransferMode",
            DnlibUtils.ReturnUInt32Method(1) // Return None (value = 1)
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Core.GlobalSettingsObject",
            "set_FASTATransferMode",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.EnumFASTATransferMode)System.Void",
            method =>
            {
                method.ReplaceWith([OpCodes.Ret.ToInstruction()]);
                method.Body.Variables.Clear();
                method.Body.ExceptionHandlers.Clear();
            }
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Core.GlobalSettingsObject",
            "TryLoadFromRegistry",
            "()\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Core.GlobalSettingsObject",
            ChangeFASTATransferModeInitialization
        );

        static void ChangeFASTATransferModeInitialization(MethodDef method)
        {
            // Find the instruction: ldc.i4.0 (BITSUpload = 0)
            // We need to change it to: ldc.i4.1 (None = 1)
            var instructions = method.Body.Instructions;

            // Look for the pattern where EnumFASTATransferMode is initialized
            // The variable is typically loaded with ldc.i4.0 and stored
            for (int i = 0; i < instructions.Count - 1; i++)
            {
                var current = instructions[i];
                var next = instructions[i + 1];

                // Look for ldc.i4.0 followed by stloc (storing to enumFASTATransferMode variable)
                if (current.OpCode == OpCodes.Ldc_I4_0 &&
                    (next.OpCode == OpCodes.Stloc_0 || next.OpCode == OpCodes.Stloc_1 ||
                     next.OpCode == OpCodes.Stloc_2 || next.OpCode == OpCodes.Stloc_3 ||
                     next.OpCode == OpCodes.Stloc_S || next.OpCode == OpCodes.Stloc))
                {
                    // Check if this is near the beginning of the method (within first ~10 instructions after try block)
                    if (i < 15)
                    {
                        // Change BITSUpload (0) to None (1)
                        current.OpCode = OpCodes.Ldc_I4_1;
                        return;
                    }
                }
            }

            Log.Warning("Could not find EnumFASTATransferMode initialization pattern in {Method}", method.FullName);
        }
    }

    [MarketLanguagePatch]
    [LibraryName("RheingoldISPINext.dll")]
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
    [LibraryName("RheingoldCoreFramework.dll")]
    public static int PatchConfigSettings(ModuleDefMD module)
    {
        return module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "IsILeanActive",
            DnlibUtils.ReturnFalseMethod
        ) + module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "IsOssModeActive",
            DnlibUtils.ReturnFalseMethod
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "ShouldUseIdentNuget",
            "(System.Boolean)System.Boolean",
            DnlibUtils.ReturnFalseMethod
        ) + module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "PsdzWebserviceEnabled",
            ReturnNullableFalse
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings",
            "GetActivateSdpOnlinePatch",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );

        static void ReturnNullableFalse(MethodDef method)
        {
            var ctor = method.FindOperand<MemberRef>(OpCodes.Newobj, "System.Void System.Nullable`1<System.Boolean>::.ctor(System.Boolean)");
            if (ctor == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            method.ReplaceWith([
                OpCodes.Ldc_I4_0.ToInstruction(),
                OpCodes.Newobj.ToInstruction(ctor),
                OpCodes.Ret.ToInstruction(),
            ]);
            method.Body.Variables.Clear();
        }
    }

    [UserAuthPatch]
    [LibraryName("RheingoldPresentationFramework.dll")]
    [FromVersion("4.44")]
    [UntilVersion("4.52")]
    public static int PatchUserEnvironmentProviderFrom444(ModuleDefMD module)
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
    [LibraryName("\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.dll")]
    [FromVersion("4.52")]
    [UntilVersion("4.55")]
    public static int PatchUserEnvironmentProviderFrom452(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.DataProviders.UserEnvironmentProvider",
            "GetCurrentUserEnvironment",
            "()\u0042\u004d\u0057.ISPI.TRIC.ISTA.LoginRepository.Entities.UserEnvironment",
            DnlibUtils.ReturnUInt32Method(2) // PROD
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.DataProviders.UserEnvironmentProvider",
            "GetCurrentNetworkType",
            "()\u0042\u004d\u0057.ISPI.TRIC.ISTA.LoginRepository.Entities.NetworkType",
            DnlibUtils.ReturnUInt32Method(1) // LAN
        );
    }

    [UserAuthPatch]
    [LibraryName("\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.dll")]
    [FromVersion("4.55")]
    public static int PatchUserEnvironmentProviderFrom455(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.DataProviders.UserEnvironmentProvider",
            "GetCurrentUserEnvironment",
            "()\u0042\u004d\u0057.ISPI.TRIC.ISTA.Contracts.Enums.UserLogin.UserEnvironment",
            DnlibUtils.ReturnUInt32Method(2) // PROD
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.DataProviders.UserEnvironmentProvider",
            "GetCurrentNetworkType",
            "()\u0042\u004d\u0057.ISPI.TRIC.ISTA.Contracts.Enums.UserLogin.NetworkType",
            DnlibUtils.ReturnUInt32Method(1) // LAN
        );
    }

    [UserAuthPatch]
    [LibraryName("RheingoldPresentationFramework.dll")]
    [FromVersion("4.48")]
    [UntilVersion("4.52")]
    public static int PatchLoginOptionsProvider(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.PresentationFramework.AuthenticationRefactored.Services.LoginOptionsProvider",
            "IsLoginEnabled",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [UserAuthPatch]
    [LibraryName("RheingoldPresentationFramework.dll")]
    [FromVersion("4.52")]
    public static int LoginEnabledOptionProvider(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.PresentationFramework.AuthenticationRefactored.Services.LoginEnabledOptionProvider",
            "IsLoginEnabled",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [SyncClientConfig]
    [LibraryName("CommonServices.dll")]
    [FromVersion("4.46")]
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
    [LibraryName("RheingoldProgramming.dll")]
    public static int PatchRetrieveActualSwtInfoState(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.RetrieveActualSwtInfoState",
            "Handle",
            "(\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.ProgrammingSession)\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.DefaultStateResult",
            SetPeriodicalCheck
        );

        static void SetPeriodicalCheck(MethodDef method)
        {
            const string oldSignature = "\u0042\u004d\u0057.Rheingold.Psdz.Model.Swt.IPsdzSwtAction \u0042\u004d\u0057.Rheingold.Psdz.IProgrammingService::RequestSwtAction(\u0042\u004d\u0057.Rheingold.Psdz.Model.IPsdzConnection,System.Boolean)";

            const string newSignature = "RheingoldPsdzWebApi.Adapter.Contracts.Model.Swt.IPsdzSwtAction RheingoldPsdzWebApi.Adapter.Contracts.Services.IProgrammingService::RequestSwtAction(RheingoldPsdzWebApi.Adapter.Contracts.Model.IPsdzConnection,System.Boolean)";

            var indexOfRequestSwtAction = method.FindIndexOfInstruction(OpCodes.Callvirt, oldSignature);

            if (indexOfRequestSwtAction == -1)
            {
                indexOfRequestSwtAction = method.FindIndexOfInstruction(OpCodes.Callvirt, newSignature);
            }

            if (indexOfRequestSwtAction == -1)
            {
                Log.Warning("Required instructions not found (neither old nor new signature), can not patch {Method}", method.FullName);
                return;
            }

            var ldcI4One = method.Body.Instructions[indexOfRequestSwtAction - 1];
            if (!ldcI4One.IsLdcI4())
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            ldcI4One.OpCode = OpCodes.Ldc_I4_0;
        }
    }

    [DisableFakeFSCRejectPatch]
    [LibraryName("RheingoldProgramming.dll")]
    public static int PatchRetrieveActualSwtEnablingCodesState(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.RetrieveActualSwtEnablingCodesState",
            "Handle",
            "(\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.ProgrammingSession)\u0042\u004d\u0057.Rheingold.Programming.ProgrammingEngine.States.DefaultStateResult",
            SetPeriodicalCheck
        );

        static void SetPeriodicalCheck(MethodDef method)
        {
            var instructions = method.Body.Instructions;

            const string oldSignature = "\u0042\u004d\u0057.Rheingold.Psdz.Model.Swt.IPsdzSwtAction \u0042\u004d\u0057.Rheingold.Psdz.IProgrammingService::RequestSwtAction(\u0042\u004d\u0057.Rheingold.Psdz.Model.IPsdzConnection,System.Boolean)";

            const string newSignature = "RheingoldPsdzWebApi.Adapter.Contracts.Model.Swt.IPsdzSwtAction RheingoldPsdzWebApi.Adapter.Contracts.Services.IProgrammingService::RequestSwtAction(RheingoldPsdzWebApi.Adapter.Contracts.Model.IPsdzConnection,System.Boolean)";

            var indexOfRequestSwtAction = method.FindIndexOfInstruction(OpCodes.Callvirt, oldSignature);

            if (indexOfRequestSwtAction == -1)
            {
                indexOfRequestSwtAction = method.FindIndexOfInstruction(OpCodes.Callvirt, newSignature);
            }

            if (indexOfRequestSwtAction == -1)
            {
                Log.Warning("Required instructions not found (neither old nor new signature), can not patch {Method}", method.FullName);
                return;
            }

            var ldcI4One = instructions[indexOfRequestSwtAction - 1];
            if (!ldcI4One.IsLdcI4())
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            ldcI4One.OpCode = OpCodes.Ldc_I4_0;
        }
    }

    [EnableAirClientPatch]
    [LibraryName("ISTAGUI.exe")]
    public static int PatchISTAGUIViewModel(ModuleDefMD module)
    {
        string[] targetTypes = [
            "MainWindowIconBarViewModel",
            "FaultPatternViewModel",
            "TestPlanViewModel",
            "TraversableDiagnosisObjectTreeViewModel.TraversableDiagnosisObjectTreeViewModel",
            "HitListViewModel",
            "ServiceConsultingViewModel",
            "FaultMemoryViewModelBase"
        ];

        return targetTypes.Sum(viewModel => module.PatchGetter($"\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.{viewModel}", "IsAirActive", DnlibUtils.ReturnTrueMethod));
    }

    [EnableAirClientPatch]
    [LibraryName("RheingoldISPINext.dll")]
    public static int PatchAirForkServicesWrapper(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldISPINext.AIR.AirForkServicesWrapper",
            "GetAirLauncher",
            "(\u0042\u004d\u0057.ISPI.IstaServices.Contract.ICS.IIstaIcsService)\u0042\u004d\u0057.ISPI.AIR.AIRClient.AirForkServices.IAirLauncher",
            ReplaceCondition) +
        module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldISPINext.AIR.AirForkServicesWrapper",
            "GetAirLauncher",
            "(\u0042\u004d\u0057.ISPI.IstaServices.Contract.ICS.IIstaIcsService)\u0042\u004d\u0057.ISPI.TRIC.ISTA.AiRForkServices.IAirLauncher",
            ReplaceCondition
        );

        static void ReplaceCondition(MethodDef method)
        {
            var indexOfCallIsILeanActive = method.FindIndexOfInstruction(OpCodes.Call, "System.Boolean \u0042\u004d\u0057.Rheingold.CoreFramework.ConfigSettings::get_IsILeanActive()");
            if (indexOfCallIsILeanActive == -1)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            var instruction = method.Body.Instructions[indexOfCallIsILeanActive];
            instruction.OpCode = OpCodes.Ldc_I4_1;
            instruction.Operand = null;
        }
    }

    [DisableBrandCompatibleCheckPatch]
    [LibraryName("RheingoldCoreFramework.dll")]
    public static int PatchBrandMapping(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.Dealer.BrandMapping",
            "IsVehicleInRange", // isSelectedBrandCompatible
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.UiBrand,\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.Vehicle)System.Boolean",
            DnlibUtils.ReturnTrueMethod);
    }

    [FixDS2VehicleIdentificationPatch]
    [LibraryName("RheingoldDiagnostics.dll")]
    [FromVersion("4.49")]
    [UntilVersion("4.56")]
    public static int PatchFixDS2VehicleIdentFrom449(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent",
            "doVehicleShortTest",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.IProgressMonitor)System.Boolean",
            FixCondition) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent",
            "DoVehicleShortTest",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.IProgressMonitor)System.Boolean",
            FixCondition);

        static void FixCondition(MethodDef method)
        {
            var instructions = method.Body.Instructions;

            var handleMissingEcusInstructions = method.FindInstructions(OpCodes.Call, "System.Void \u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent::HandleMissingEcus(System.Boolean)");
            var handleMissingEcusProcessed = false;
            foreach (var handleMissingEcus in handleMissingEcusInstructions)
            {
                var indexOfHandleMissingEcus = instructions.IndexOf(handleMissingEcus);

                // make sure statement is `HandleMissingEcus(false)` and remove it
                if (instructions[indexOfHandleMissingEcus - 1].OpCode == OpCodes.Ldc_I4_0)
                {
                    instructions[indexOfHandleMissingEcus] = OpCodes.Nop.ToInstruction();
                    instructions[indexOfHandleMissingEcus - 1] = OpCodes.Nop.ToInstruction();
                    instructions[indexOfHandleMissingEcus - 2] = OpCodes.Nop.ToInstruction();
                    handleMissingEcusProcessed = true;
                    break;
                }
            }

            if (!handleMissingEcusProcessed)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            var indexOfSetIdentSuccessfully = method.FindIndexOfInstruction(OpCodes.Callvirt, "System.Void \u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.ECU::set_IDENT_SUCCESSFULLY(System.Boolean)");
            var getVecInfo = method.FindOperand<MethodDef>(OpCodes.Call, "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.Vehicle \u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent::get_VecInfo()");
            var getBNType = method.FindOperand<MemberRef>(OpCodes.Callvirt, "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.BNType \u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.typeVehicle::get_BNType()");
            if (indexOfSetIdentSuccessfully == -1 || getVecInfo == null || getBNType == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // set IDENT_SUCCESSFULLY only if BNType is BNType.IBUS
            Instruction[] ifConditions =
            [
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getVecInfo),
                OpCodes.Callvirt.ToInstruction(getBNType),
                OpCodes.Ldc_I4.ToInstruction(2),
                OpCodes.Beq_S.ToInstruction(instructions[indexOfSetIdentSuccessfully + 1]),
            ];

            foreach (var instruction in ifConditions.Reverse())
            {
                instructions.Insert(indexOfSetIdentSuccessfully - 2, instruction);
            }
        }
    }

    [FixDS2VehicleIdentificationPatch]
    [LibraryName("RheingoldDiagnostics.dll")]
    [FromVersion("4.56")]
    public static int PatchFixDS2VehicleIdentFrom456(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent",
            "DoVehicleShortTest",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.IProgressMonitor)System.Boolean",
            FixDoVehicleShortTest);

        static void FixDoVehicleShortTest(MethodDef method)
        {
            var instructions = method.Body.Instructions;

            var indexHandleMissingEcusCall = -1;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Call &&
                    instructions[i].Operand is IMethod m &&
                    m.Name == "HandleMissingEcus" &&
                    m.MethodSig?.Params.Count == 0)
                {
                    indexHandleMissingEcusCall = i;
                    break;
                }
            }

            if (indexHandleMissingEcusCall != -1)
            {
                instructions[indexHandleMissingEcusCall] = OpCodes.Nop.ToInstruction();
                instructions[indexHandleMissingEcusCall - 1] = OpCodes.Nop.ToInstruction();
            }

            var indexSetIdent = -1;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Callvirt &&
                    instructions[i].Operand is IMethod m &&
                    m.Name == "set_IDENT_SUCCESSFULLY" &&
                    i >= 2 &&
                    instructions[i - 1].OpCode == OpCodes.Ldc_I4_0 &&
                    instructions[i - 2].OpCode == OpCodes.Ldloc_S)
                {
                    indexSetIdent = i;
                    break;
                }
            }

            if (indexSetIdent == -1)
            {
                Log.Warning("Could not find set_IDENT_SUCCESSFULLY in foreach loop in {Method}", method.FullName);
                return;
            }

            var getVecInfo = method.FindOperand<MethodDef>(
                OpCodes.Call,
                "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.Vehicle \u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent::get_VecInfo()");
            var getBNType = method.FindOperand<MemberRef>(
                OpCodes.Callvirt,
                "\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.BNType \u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.typeVehicle::get_BNType()");

            if (getVecInfo == null || getBNType == null)
            {
                Log.Warning("Required method references not found in {Method}", method.FullName);
                return;
            }

            Instruction skipTarget = instructions[indexSetIdent + 1];

            List<Instruction> conditionInstructions = new List<Instruction>
            {
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getVecInfo),
                OpCodes.Callvirt.ToInstruction(getBNType),
                OpCodes.Ldc_I4_2.ToInstruction(),
                OpCodes.Beq_S.ToInstruction(skipTarget),
            };

            int insertIndex = indexSetIdent - 2;
            foreach (var instr in conditionInstructions.AsEnumerable().Reverse())
            {
                instructions.Insert(insertIndex, instr);
            }

            method.Body.SimplifyBranches();
            method.Body.OptimizeBranches();
        }
    }

    [ForceICOMNextPatch]
    [LibraryName("RheingoldxVM.dll")]
    public static int PatchSLP(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.xVM.SLP",
            "ScanDeviceFromAttrList",
            "(\u0042\u004d\u0057.Rheingold.xVM.SLPAttrRply,System.String[])\u0042\u004d\u0057.Rheingold.CoreFramework.DatabaseProvider.VCIDevice",
            ReplaceDeviceType) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.xVM.SLP",
            "IsIcomUnsupported",
            "(System.Collections.Generic.Dictionary`2<System.String,System.String>)System.Boolean",
            DnlibUtils.ReturnFalseMethod);

        static void ReplaceDeviceType(MethodDef method)
        {
            const string targetOperand = "System.String System.Collections.Generic.Dictionary`2<System.String,System.String>::get_Item(System.String)";
            var instructions = method.FindInstructions(OpCodes.Ldstr, "DevTypeExt");
            foreach (var instruction in instructions)
            {
                var idx = method.Body.Instructions.IndexOf(instruction);
                var prevInstruction = method.Body.Instructions[idx - 1];
                var nextInstruction = method.Body.Instructions[idx + 1];
                if (
                    prevInstruction.OpCode == OpCodes.Ldloc_0 &&
                    nextInstruction.OpCode == OpCodes.Callvirt && string.Equals((nextInstruction.Operand as IMethod)?.FullName, targetOperand, StringComparison.Ordinal))
                {
                    instruction.Operand = "ICOM_Next_A";
                    method.Body.Instructions.Remove(prevInstruction);
                    method.Body.Instructions.Remove(nextInstruction);
                    break;
                }
            }
        }
    }

    [MotorbikeClamp15Patch]
    [LibraryName("RheingoldDiagnostics.dll")]
    public static int PatchMotorbikeClamp15(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Diagnostics.VehicleIdent",
            "ClearAndReadErrorInfoMemory",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.Contracts.IJobServices)System.Void",
            PatchClamp15Check
        );

        static void PatchClamp15Check(MethodDef method)
        {
            var instructions = method.Body.Instructions;

            // Transform the logic from:
            // Original: if (flag && (!clamp.HasValue || clamp < 0.1)) { RegisterMessage(); return; }
            // To: if (flag && (clamp.HasValue && clamp < 0.1)) { RegisterMessage(); return; }

            // First find the HasValue call
            var hasValueCall = method.FindInstruction(OpCodes.Call, "System.Boolean System.Nullable`1<System.Double>::get_HasValue()");
            if (hasValueCall == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            var hasValueIndex = instructions.IndexOf(hasValueCall);
            if (hasValueIndex == -1 || hasValueIndex >= instructions.Count - 1)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // The next instruction after HasValue call should be brfalse.s
            var brfalseInstruction = instructions[hasValueIndex + 1];
            if (brfalseInstruction.OpCode != OpCodes.Brfalse_S)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Find the instruction that skips the RegisterMessage block (IL_012d in the original)
            // This should be the target of the second brfalse.s instruction after the value comparison
            var valueComparisonBrfalse = instructions.Skip(hasValueIndex + 2)
                .FirstOrDefault(inst => inst.OpCode == OpCodes.Brfalse_S);

            if (valueComparisonBrfalse == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            // Change the first brfalse target to point to the same target as the value comparison brfalse
            // This transforms (!clamp.HasValue || clamp < 0.1) to (clamp.HasValue && clamp < 0.1)
            brfalseInstruction.Operand = valueComparisonBrfalse.Operand;
        }
    }

    [ManualClampSwitchPatch]
    [LibraryName("RheingoldDiagnostics.dll")]
    public static int PatchManualClampSwitch(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.Diagnostics.ClampSwitchVehicle",
            "ManualClampSwitch",
            "(\u0042\u004d\u0057.Rheingold.CoreFramework.Interaction.Models.InteractionVehicleIgnitionModel,System.Boolean,System.String)System.Void",
            ModifyClampSwitchLogic
        );

        static void ModifyClampSwitchLogic(MethodDef method)
        {
            var instructions = method.Body.Instructions;

            int tryStartIdx = -1;
            object? vehicleAccess = null;
            IMethod? getVci = null;
            IMethod? getVciType = null;

            for (int i = 0; i < instructions.Count - 5; i++)
            {
                if (instructions[i].OpCode == OpCodes.Ldarg_0 &&
                    (instructions[i + 1].OpCode == OpCodes.Ldfld || instructions[i + 1].OpCode == OpCodes.Callvirt) &&
                    instructions[i + 2].OpCode == OpCodes.Callvirt &&
                    instructions[i + 3].OpCode == OpCodes.Callvirt &&
                    instructions[i + 4].IsLdcI4() && instructions[i + 4].GetLdcI4Value() == 7 &&
                    instructions[i + 5].OpCode == OpCodes.Beq)
                {
                    tryStartIdx = i;
                    vehicleAccess = instructions[i + 1].Operand;
                    getVci = instructions[i + 2].Operand as IMethod;
                    getVciType = instructions[i + 3].Operand as IMethod;
                    break;
                }
            }

            if (tryStartIdx == -1 || vehicleAccess == null || getVci == null || getVciType == null)
            {
                Log.Warning("Could not find VCIType check pattern in {Method}", method.FullName);
                return;
            }

            Instruction vehicleInstruction = instructions[tryStartIdx + 1].OpCode == OpCodes.Ldfld
                ? OpCodes.Ldfld.ToInstruction(vehicleAccess as IField)
                : OpCodes.Callvirt.ToInstruction(vehicleAccess as IMethod);

            var module = method.Module;

            var instructionList = method.Body.Instructions;
            IMethod arrayEmpty = null;
            IMethod logInfo = null;
            IMethod setStep = null;
            IMethod waitForResponse = null;
            IMethod getResponse = null;
            IMethod cancel = null;
            IMethod getTitle = null;
            IMethod localize = null;
            IMethod progressWaitCtor = null;
            IMethod register = null;
            IMethod checkForAutoSkip = null;
            IField getInteractionService = null;

            foreach (var instr in instructionList)
            {
                if (instr.Operand is IMethod m)
                {
                    if (m.Name == "Empty" && m.DeclaringType?.Name == "Array")
                    {
                        arrayEmpty = m;
                    }
                    else if (m.Name == "Info" && m.DeclaringType?.Name == "Log")
                    {
                        logInfo = m;
                    }
                    else if (m.Name == "set_Step")
                    {
                        setStep = m;
                    }
                    else if (m.Name == "WaitForResponse")
                    {
                        waitForResponse = m;
                    }
                    else if (m.Name == "get_Response")
                    {
                        getResponse = m;
                    }
                    else if (m.Name == "Cancel" && m.DeclaringType?.Name == "CancellationTokenSource")
                    {
                        cancel = m;
                    }
                    else if (m.Name == "get_Title")
                    {
                        getTitle = m;
                    }
                    else if (m.Name == "Localize")
                    {
                        localize = m;
                    }
                    else if (m.Name == ".ctor" && m.DeclaringType?.Name == "InteractionProgressWaitModel")
                    {
                        progressWaitCtor = m;
                    }
                    else if (m.Name == "Register")
                    {
                        register = m;
                    }
                    else if (m.Name == "CheckForAutoSkip")
                    {
                        checkForAutoSkip = m;
                    }
                }
                else if (instr.Operand is IField f)
                {
                    if (f.Name == "interactionService")
                    {
                        getInteractionService = f;
                    }
                }
            }

            if (arrayEmpty == null || logInfo == null || setStep == null || waitForResponse == null ||
                getResponse == null || cancel == null || getTitle == null ||
                localize == null || progressWaitCtor == null || register == null ||
                checkForAutoSkip == null || getInteractionService == null)
            {
                Log.Warning("Could not find existing method references in {Method}", method.FullName);
                return;
            }

            IField func7000 = null;
            IField func9000 = null;
            IField singletonField = null;
            IMethod lambda7000Method = null;
            IMethod lambda9000Method = null;
            IMethod funcConstructor = null;

            foreach (var instr in instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld && instr.Operand is IField f)
                {
                    var fieldSig = f.FieldSig;
                    if (fieldSig?.Type?.TypeName?.Contains("Func") == true)
                    {
                        if (func7000 == null)
                        {
                            func7000 = f;
                        }
                        else if (func9000 == null)
                        {
                            func9000 = f;
                            break;
                        }
                    }
                }
            }

            if (func7000 == null || func9000 == null)
            {
                Log.Warning("Lambda functions for voltage checks not found in {Method}", method.FullName);
                return;
            }

            var compilerGeneratedClassDef = func7000.DeclaringType.ResolveTypeDef();
            if (compilerGeneratedClassDef != null)
            {
                foreach (var field in compilerGeneratedClassDef.Fields)
                {
                    if (field.IsStatic && field.FieldType.FullName == compilerGeneratedClassDef.FullName)
                    {
                        singletonField = field;
                        break;
                    }
                }
            }

            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Ldftn && instructions[i].Operand is IMethod lambdaMethod)
                {
                    bool isCheckForAutoSkipContext = false;
                    for (int j = i; j < Math.Min(i + 10, instructions.Count); j++)
                    {
                        if (instructions[j].OpCode == OpCodes.Call && instructions[j].Operand is IMethod calledMethod)
                        {
                            if (calledMethod.Name == "CheckForAutoSkip")
                            {
                                isCheckForAutoSkipContext = true;
                                break;
                            }
                        }
                    }

                    if (!isCheckForAutoSkipContext)
                    {
                        continue;
                    }

                    var methodSig = lambdaMethod.MethodSig;
                    if (methodSig != null &&
                        methodSig.RetType.TypeName == "Boolean" &&
                        methodSig.Params.Count == 1 &&
                        methodSig.Params[0].TypeName == "Double")
                    {
                        if (lambdaMethod is MethodDef lambdaMethodDef && lambdaMethodDef.HasBody)
                        {
                            var lambdaInstructions = lambdaMethodDef.Body.Instructions;
                            bool is7000Lambda = false;
                            bool is9000Lambda = false;

                            foreach (var lambdaInstr in lambdaInstructions)
                            {
                                if (lambdaInstr.OpCode == OpCodes.Ldc_R8 && lambdaInstr.Operand is double constValue)
                                {
                                    if (Math.Abs(constValue - 7000.0) < 0.1)
                                    {
                                        is7000Lambda = true;
                                    }
                                    else if (Math.Abs(constValue - 9000.0) < 0.1)
                                    {
                                        is9000Lambda = true;
                                    }
                                }
                            }

                            if (is7000Lambda)
                            {
                                lambda7000Method = lambdaMethod;
                            }
                            else if (is9000Lambda)
                            {
                                lambda9000Method = lambdaMethod;
                            }

                            if (lambda7000Method != null && lambda9000Method != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var instr in instructions)
            {
                if (instr.OpCode == OpCodes.Newobj && instr.Operand is IMethod ctor)
                {
                    if (ctor.DeclaringType?.FullName.Contains("System.Func") == true &&
                        ctor.DeclaringType.FullName.Contains("System.Double") &&
                        ctor.DeclaringType.FullName.Contains("System.Boolean"))
                    {
                        funcConstructor = ctor;
                        break;
                    }
                }
            }

            if (singletonField == null || lambda7000Method == null || lambda9000Method == null || funcConstructor == null)
            {
                Log.Warning("Could not find all required components for lazy initialization in {Method}", method.FullName);
                return;
            }

            List<Instruction> CreateLazyInitPattern(IField lambdaField, IMethod lambdaMethod, Instruction skipInit)
            {
                return new List<Instruction>
                {
                    OpCodes.Ldsfld.ToInstruction(lambdaField),
                    OpCodes.Dup.ToInstruction(),
                    OpCodes.Brtrue_S.ToInstruction(skipInit),
                    OpCodes.Pop.ToInstruction(),
                    OpCodes.Ldsfld.ToInstruction(singletonField),
                    OpCodes.Ldftn.ToInstruction(lambdaMethod),
                    OpCodes.Newobj.ToInstruction(funcConstructor),
                    OpCodes.Dup.ToInstruction(),
                    OpCodes.Stsfld.ToInstruction(lambdaField),
                };
            }

            var skipToOriginalCheck = OpCodes.Nop.ToInstruction();

            var skipInit7000 = OpCodes.Nop.ToInstruction();
            var skipInit9000 = OpCodes.Nop.ToInstruction();

            var leaveInstruction = instructions.FirstOrDefault(i => i.OpCode == OpCodes.Leave || i.OpCode == OpCodes.Leave_S);
            if (leaveInstruction == null)
            {
                Log.Warning("Could not find leave instruction in {Method}", method.FullName);
                return;
            }

            var type3Block = new List<Instruction>
            {
                OpCodes.Ldarg_0.ToInstruction(),
                vehicleInstruction,
                OpCodes.Callvirt.ToInstruction(getVci),
                OpCodes.Callvirt.ToInstruction(getVciType),
                OpCodes.Ldc_I4_3.ToInstruction(),
                OpCodes.Bne_Un.ToInstruction(skipToOriginalCheck),
                OpCodes.Ldstr.ToInstruction("ClampSwitchVehicle.ManualClampSwitch()"),
                OpCodes.Ldstr.ToInstruction("Start clamp switch to state off."),
                OpCodes.Call.ToInstruction(arrayEmpty),
                OpCodes.Call.ToInstruction(logInfo),
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Ldc_I4_0.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setStep),
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldarg_1.ToInstruction(),
            };

            type3Block.AddRange(CreateLazyInitPattern(func7000, lambda7000Method, skipInit7000));
            type3Block.Add(skipInit7000);
            type3Block.Add(OpCodes.Call.ToInstruction(checkForAutoSkip));

            type3Block.AddRange(new[]
            {
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(waitForResponse),
                OpCodes.Callvirt.ToInstruction(cancel),
                OpCodes.Ldstr.ToInstruction("ClampSwitchVehicle.ManualClampSwitch()"),
                OpCodes.Ldstr.ToInstruction("Clamp switch to state off finished."),
                OpCodes.Call.ToInstruction(arrayEmpty),
                OpCodes.Call.ToInstruction(logInfo),
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(getResponse),
                OpCodes.Pop.ToInstruction(),
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldfld.ToInstruction(getInteractionService),
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(getTitle),
                OpCodes.Ldstr.ToInstruction("#WaitForChangeover"),
                OpCodes.Call.ToInstruction(localize),
                OpCodes.Ldc_I4.ToInstruction(10000),
                OpCodes.Newobj.ToInstruction(progressWaitCtor),
                OpCodes.Callvirt.ToInstruction(register),
                OpCodes.Ldstr.ToInstruction("ClampSwitchVehicle.ManualClampSwitch()"),
                OpCodes.Ldstr.ToInstruction("Start clamp switch to state on."),
                OpCodes.Call.ToInstruction(arrayEmpty),
                OpCodes.Call.ToInstruction(logInfo),
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setStep),
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldarg_1.ToInstruction(),
            });

            type3Block.AddRange(CreateLazyInitPattern(func9000, lambda9000Method, skipInit9000));
            type3Block.Add(skipInit9000);
            type3Block.Add(OpCodes.Call.ToInstruction(checkForAutoSkip));

            type3Block.AddRange(new[]
            {
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(waitForResponse),
                OpCodes.Callvirt.ToInstruction(cancel),
                OpCodes.Ldstr.ToInstruction("ClampSwitchVehicle.ManualClampSwitch()"),
                OpCodes.Ldstr.ToInstruction("Clamp switch to state on finished."),
                OpCodes.Call.ToInstruction(arrayEmpty),
                OpCodes.Call.ToInstruction(logInfo),
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(getResponse),
                OpCodes.Pop.ToInstruction(),
            });

            type3Block.Add(OpCodes.Leave.ToInstruction(leaveInstruction.Operand as Instruction));

            for (int i = 0; i < type3Block.Count; i++)
            {
                instructions.Insert(tryStartIdx + i, type3Block[i]);
            }

            instructions.Insert(tryStartIdx + type3Block.Count, skipToOriginalCheck);

            foreach (var eh in method.Body.ExceptionHandlers)
            {
                if (eh.TryStart != null && instructions.IndexOf(eh.TryStart) >= tryStartIdx)
                {
                    eh.TryStart = instructions[tryStartIdx];
                }
            }

            method.Body.SimplifyBranches();
            method.Body.OptimizeBranches();
        }
    }
}
