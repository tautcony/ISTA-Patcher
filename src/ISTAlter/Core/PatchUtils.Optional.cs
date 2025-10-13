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
    public static int PatchLogic(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic",
            "SendFastaDataToFBM",
            "(System.String,System.Boolean)System.String",
            DnlibUtils.ReturnStringMethod(null)
        ) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.RheingoldSessionController.Logic",
            "SendObfcmDataToBackend",
            "(System.Int32,System.Boolean,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.Double,System.String)System.Void",
            DnlibUtils.ReturnStringMethod(null)
        );
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
            var indexOfRequestSwtAction = method.FindIndexOfInstruction(OpCodes.Callvirt, "\u0042\u004d\u0057.Rheingold.Psdz.Model.Swt.IPsdzSwtAction \u0042\u004d\u0057.Rheingold.Psdz.IProgrammingService::RequestSwtAction(\u0042\u004d\u0057.Rheingold.Psdz.Model.IPsdzConnection,System.Boolean)");
            if (indexOfRequestSwtAction == -1)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
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
            var requestSwtAction = method.FindInstruction(OpCodes.Callvirt, "\u0042\u004d\u0057.Rheingold.Psdz.Model.Swt.IPsdzSwtAction \u0042\u004d\u0057.Rheingold.Psdz.IProgrammingService::RequestSwtAction(\u0042\u004d\u0057.Rheingold.Psdz.Model.IPsdzConnection,System.Boolean)");
            if (requestSwtAction == null)
            {
                Log.Warning("Required instructions not found, can not patch {Method}", method.FullName);
                return;
            }

            var indexOfRequestSwtAction = instructions.IndexOf(requestSwtAction);
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
    public static int PatchFixDS2VehicleIdent(ModuleDefMD module)
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
}
