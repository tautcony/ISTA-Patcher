// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAlter.Core;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ISTAlter.Utils;
using Serilog;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the ISTA voltage controller injection logic.
/// </summary>
/// <remarks>
/// Hooks into IstaOperation.exe's InitializeServices, loading ISTAVoltageControl.dll at
/// runtime and calling Controller.StartFromIstaOperation to enable KL15/KL30 voltage
/// tracking over OBD/D-CAN or ENET. The injection is idempotent and wrapped in a silent
/// try/catch so it never crashes ISTA.
///
/// Two external dependencies are required, neither of which is part of this repository:
/// <list type="number">
///   <item>
///     <description>
///       EdiabasLib (Api64.dll). Resolved at runtime via the BinPathModifications registry
///       key (HKLM\SOFTWARE\BMWGroup\ISPI\Rheingold\BMW.Rheingold.ISTAGUI.BinPathModifications),
///       falling back to the stock ISTA Ediabas BIN folder.
///       Source: https://github.com/uholeschak/ediabaslib.
///     </description>
///   </item>
///   <item>
///     <description>
///       ISTAVoltageControl.dll. An external voltage-controller bridge that must be deployed
///       into the folder resolved above before launching ISTA. This patch only installs the
///       bootstrap call; all voltage logic (KL15/KL30 polling, VCI walk) lives inside that
///       DLL. Its public contract is a static method
///       <c>ISTAVoltageControl.Controller.StartFromIstaOperation(object serviceImpl)</c>.
///     </description>
///   </item>
/// </list>
/// If the flag is used but either dependency is missing, the silent try/catch ensures ISTA
/// continues to start normally.
/// </remarks>
public static partial class PatchUtils
{
    private const string ISTAControllerAssembly = "ISTAVoltageControl.dll";
    private const string ISTAControllerType = "ISTAVoltageControl.Controller";
    private const string ISTAControllerEntry = "StartFromIstaOperation";

    private const string ISTADefaultEdiabasBin = @"..\..\..\Ediabas\BIN";

    [ISTAVoltagePatch]
    [LibraryName("IstaOperation.exe")]
    public static int PatchISTAVoltageControl(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.ISPI.IstaOperation.IstaOperationProcess",
            "InitializeServices",
            "(System.DateTime,System.String,System.String,\u0042\u004d\u0057.ISPI.IstaOperation.Contract.IstaOperationArgs)System.Void",
            InjectVoltageControl
        );

        static void InjectVoltageControl(MethodDef method)
        {
            var instructions = method.Body.Instructions;

            if (instructions.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == ISTAControllerType))
            {
                return;
            }

            var module = method.Module;
            var corlib = module.CorLibTypes;
            var corlibAsm = corlib.AssemblyRef;

            TypeRefUser TR(string @namespace, string name) => new(module, @namespace, name, corlibAsm);

            var typeRef = TR("System", "Type");
            var assemblyRef = TR("System.Reflection", "Assembly");
            var methodInfoRef = TR("System.Reflection", "MethodInfo");
            var methodBaseRef = TR("System.Reflection", "MethodBase");
            var bindingFlagsRef = TR("System.Reflection", "BindingFlags");
            var appDomainRef = TR("System", "AppDomain");
            var pathRef = TR("System.IO", "Path");
            var exceptionRef = TR("System", "Exception");

            var typeSig = typeRef.ToTypeSig();
            var assemblySig = assemblyRef.ToTypeSig();
            var methodInfoSig = methodInfoRef.ToTypeSig();
            var bindingFlagsSig = new ValueTypeSig(bindingFlagsRef); // enum -> must be encoded as value type
            var appDomainSig = appDomainRef.ToTypeSig();

            var getMethodRef = new MemberRefUser(module, "GetMethod", MethodSig.CreateInstance(methodInfoSig, corlib.String, bindingFlagsSig), typeRef);
            var invokeRef = new MemberRefUser(module, "Invoke", MethodSig.CreateInstance(corlib.Object, corlib.Object, new SZArraySig(corlib.Object)), methodBaseRef);
            var loadFileRef = new MemberRefUser(module, "LoadFile", MethodSig.CreateStatic(assemblySig, corlib.String), assemblyRef);
            var asmGetTypeRef = new MemberRefUser(module, "GetType", MethodSig.CreateInstance(typeSig, corlib.String), assemblyRef);
            var getCurrentDomainRef = new MemberRefUser(module, "get_CurrentDomain", MethodSig.CreateStatic(appDomainSig), appDomainRef);
            var getBaseDirRef = new MemberRefUser(module, "get_BaseDirectory", MethodSig.CreateInstance(corlib.String), appDomainRef);
            var combineRef = new MemberRefUser(module, "Combine", MethodSig.CreateStatic(corlib.String, corlib.String, corlib.String), pathRef);

            var coreAsm = module.GetAssemblyRefs().FirstOrDefault(a => a.Name == "RheingoldCoreFramework");
            if (coreAsm == null)
            {
                Log.Warning("RheingoldCoreFramework reference not found, can not patch {Method}", method.FullName);
                return;
            }

            var configSettingsRef = new TypeRefUser(module, "\u0042\u004d\u0057.Rheingold.CoreFramework", "ConfigSettings", coreAsm);
            var getPathStringRef = new MemberRefUser(module, "getPathString", MethodSig.CreateStatic(corlib.String, corlib.String, corlib.String), configSettingsRef);

            const int bfStaticPublic = 24; // BindingFlags.Static | BindingFlags.Public

            var anchor = instructions.FirstOrDefault(i =>
                i.OpCode == OpCodes.Callvirt && i.Operand is IMethod m && m.Name == "SetOperationStartTime");
            if (anchor == null)
            {
                Log.Warning("SetOperationStartTime anchor not found, can not patch {Method}", method.FullName);
                return;
            }

            var anchorIndex = instructions.IndexOf(anchor);
            if (anchorIndex < 0 || anchorIndex + 1 >= instructions.Count)
            {
                Log.Warning("Unexpected InitializeServices layout, can not patch {Method}", method.FullName);
                return;
            }

            var continuation = instructions[anchorIndex + 1];
            var serviceImplLocal = method.Body.Variables[1]; // IstaOperationServiceImpl

            var tryStart = OpCodes.Call.ToInstruction(getCurrentDomainRef);
            var handlerStart = OpCodes.Pop.ToInstruction();

            var injected = new List<Instruction>
            {
                tryStart,
                OpCodes.Callvirt.ToInstruction(getBaseDirRef),
                OpCodes.Ldstr.ToInstruction("\u0042\u004d\u0057.Rheingold.ISTAGUI.BinPathModifications"),
                OpCodes.Ldstr.ToInstruction(ISTADefaultEdiabasBin),
                OpCodes.Call.ToInstruction(getPathStringRef),
                OpCodes.Call.ToInstruction(combineRef),
                OpCodes.Ldstr.ToInstruction(ISTAControllerAssembly),
                OpCodes.Call.ToInstruction(combineRef),

                OpCodes.Call.ToInstruction(loadFileRef),
                OpCodes.Ldstr.ToInstruction(ISTAControllerType),
                OpCodes.Callvirt.ToInstruction(asmGetTypeRef),
                OpCodes.Ldstr.ToInstruction(ISTAControllerEntry),
                Instruction.CreateLdcI4(bfStaticPublic),
                OpCodes.Callvirt.ToInstruction(getMethodRef),

                OpCodes.Ldnull.ToInstruction(),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Newarr.ToInstruction(corlib.Object.TypeDefOrRef),
                OpCodes.Dup.ToInstruction(),
                OpCodes.Ldc_I4_0.ToInstruction(),
                OpCodes.Ldloc.ToInstruction(serviceImplLocal),
                OpCodes.Stelem_Ref.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(invokeRef),
                OpCodes.Pop.ToInstruction(),
                OpCodes.Leave.ToInstruction(continuation),

                handlerStart,
                OpCodes.Leave.ToInstruction(continuation),
            };

            for (var i = injected.Count - 1; i >= 0; i--)
            {
                instructions.Insert(anchorIndex + 1, injected[i]);
            }

            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = tryStart,
                TryEnd = handlerStart,
                HandlerStart = handlerStart,
                HandlerEnd = continuation,
                CatchType = exceptionRef,
            });

            method.Body.SimplifyBranches();
            method.Body.OptimizeBranches();
        }
    }
}
