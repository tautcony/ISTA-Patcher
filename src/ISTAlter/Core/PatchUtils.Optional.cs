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

        void RemoveIsProgrammingEnabledCheck(MethodDef method)
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

        void RemoveRequirementsCheck(MethodDef method)
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

        void ReturnNullableFalse(MethodDef method)
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
    [LibraryName("\u0042\u004d\u0057.ISPI.TRIC.ISTA.LOGIN.dll")]
    [FromVersion("4.52")]
    public static int PatchLoginUserEnvironmentProvider(ModuleDefMD module)
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

        void SetPeriodicalCheck(MethodDef method)
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

        void SetPeriodicalCheck(MethodDef method)
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
    public static int PatchMainWindowIconBarViewModel(ModuleDefMD module)
    {
        return module.PatchGetter(
            "\u0042\u004d\u0057.Rheingold.ISTAGUI.ViewModels.MainWindowIconBarViewModel",
            "IsAirActive",
            DnlibUtils.ReturnTrueMethod
        );
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

        void ReplaceCondition(MethodDef method)
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
            FixCondition);

        void FixCondition(MethodDef method)
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
    [UntilVersion("4.54")]
    public static int PatchSLP(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.xVM.SLP",
            "ScanDeviceFromAttrList",
            "(BMW.Rheingold.xVM.SLPAttrRply,System.String[])BMW.Rheingold.CoreFramework.DatabaseProvider.VCIDevice",
            ReplaceDeviceType) + module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.xVM.SLP",
            "IsIcomUnsupported",
            "(System.Collections.Generic.Dictionary`2<System.String,System.String>)System.Boolean",
            DnlibUtils.ReturnFalseMethod);

        void ReplaceDeviceType(MethodDef method)
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
}
