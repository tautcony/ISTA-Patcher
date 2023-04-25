// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

using System.Reflection;

namespace ISTA_Patcher;

using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

public static class DnlibUtils
{
    /// <summary>
    /// Get the description of a method.
    /// </summary>
    /// <param name="md">Method to get description.</param>
    /// <returns>Description of the method.</returns>
    public static string DescriptionOf(MethodDef md)
    {
        var sb = new StringBuilder();
        sb.Append('(');

        var set = false;
        foreach (var param in md.MethodSig.Params)
        {
            sb.Append(param.FullName);
            sb.Append(',');
            set = true;
        }

        if (set)
        {
            sb.Length -= 1;
        }

        sb.Append(')');
        sb.Append(md.ReturnType.FullName);
        return sb.ToString();
    }

    /// <summary>
    /// Get the TypeDef of a method.
    /// </summary>
    /// <param name="module">Module to get TypeDef.</param>
    /// <param name="fullName">Full name of the method.</param>
    /// <returns>TypeDef of the method.</returns>
    public static TypeDef? GetType(this ModuleDef module, string fullName)
    {
        return module.GetTypes().FirstOrDefault(tp => tp.FullName == fullName);
    }

    /// <summary>
    /// Get the MethodDef of a method from a module.
    /// </summary>
    /// <param name="asm">Assembly to get MethodDef.</param>
    /// <param name="type">Type of the method.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="desc">Description of the method.</param>
    /// <returns>MethodDef of the method.</returns>
    public static MethodDef? GetMethod(this AssemblyDef asm, string type, string name, string desc)
    {
        var td = asm.Modules.SelectMany(m => m.GetTypes()).FirstOrDefault(tp => tp.FullName == type);
        desc = desc.Replace(" ", string.Empty);
        return td?.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc));
    }

    /// <summary>
    /// Empty the given method.
    /// </summary>
    /// <param name="method">Method to empty.</param>
    /// <exception cref="Exception">Thrown when the method has no body.</exception>
    public static void EmptyingMethod(this MethodDef method)
    {
        var body = method.Body;
        if (body != null)
        {
            body.Variables.Clear();
            body.ExceptionHandlers.Clear();
            body.Instructions.Clear();
            body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else
        {
            throw new Exception($"{method.FullName}.Body null!");
        }
    }

    /// <summary>
    /// Return 0 in the given method.
    /// </summary>
    /// <param name="method">Method to return 0.</param>
    /// <exception cref="Exception">Thrown when the method has no body.</exception>
    public static void ReturnZeroMethod(this MethodDef method)
    {
        var body = method.Body;
        if (body != null)
        {
            body.Variables.Clear();
            body.ExceptionHandlers.Clear();
            body.Instructions.Clear();
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else
        {
            throw new Exception($"{method.FullName}.Body null!");
        }
    }

    /// <summary>
    /// Return 1 in the given method.
    /// </summary>
    /// <param name="method">Method to return 1.</param>
    /// <exception cref="Exception">Thrown when the method has no body.</exception>
    public static void ReturnOneMethod(this MethodDef method)
    {
        var body = method.Body;
        if (body != null)
        {
            body.Variables.Clear();
            body.ExceptionHandlers.Clear();
            body.Instructions.Clear();
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else
        {
            throw new Exception($"{method.FullName}.Body null!");
        }
    }

    /// <summary>
    /// Return false in the given method.
    /// </summary>
    /// <param name="method">Method to return false.</param>
    public static void ReturnFalseMethod(this MethodDef method)
    {
        ReturnZeroMethod(method);
    }

    /// <summary>
    /// Return true in the given method.
    /// </summary>
    /// <param name="method">Method to return true.</param>
    public static void ReturnTrueMethod(this MethodDef method)
    {
        ReturnOneMethod(method);
    }

    /// <summary>
    /// Find the first instruction with the given opCode and operand.
    /// </summary>
    /// <param name="method">Method to find instruction.</param>
    /// <param name="opCode">OpCode of the instruction.</param>
    /// <param name="operandName">Operand of the instruction.</param>
    /// <returns>Instruction with the given opCode and operand.</returns>
    public static Instruction? FindInstruction(this MethodDef method, OpCode opCode, string operandName)
    {
        return method.Body.Instructions.FirstOrDefault(instruction =>
            instruction.OpCode == opCode && (instruction.Operand as IMethod)?.FullName == operandName);
    }

    /// <summary>
    /// Find the first instruction with the given opCode and operand.
    /// </summary>
    /// <param name="method">Method to find instruction.</param>
    /// <param name="instructions">Instructions to find.</param>
    public static void ReplaceWith(this MethodDef method, IEnumerable<Instruction> instructions)
    {
        method.Body.Instructions.Clear();
        foreach (var instruction in instructions)
        {
            method.Body.Instructions.Add(instruction);
        }
    }

    public static IMethod? BuildCall(ModuleDef module, Type type, string method, Type returnType, Type[]? parameters)
    {
        var importer = new Importer(module);
        foreach (var m in type.GetMethods())
        {
            if (m.Name != method || m.ReturnType != returnType)
            {
                continue;
            }

            if (m.GetParameters().Length == 0 && parameters == null)
            {
                return importer.Import(m);
            }

            if (m.GetParameters().Length == parameters?.Length && CheckParametersByType(m.GetParameters(), parameters))
            {
                return importer.Import(m);
            }
        }

        return null;
    }

    private static bool CheckParametersByType(ParameterInfo[] parameters, Type[] types)
    {
        return !parameters.Where((t, i) => types[i] != t.ParameterType).Any();
    }
}
