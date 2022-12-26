// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022 TautCony
namespace ISTA_Patcher;

using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

public static class DnlibUtils
{
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

    public static TypeDef? GetType(this ModuleDef module, string fullName)
    {
        return module.GetTypes().FirstOrDefault(tp => tp.FullName == fullName);
    }

    public static MethodDef? GetMethod(this AssemblyDef asm, string type, string name, string desc)
    {
        var td = asm.Modules.SelectMany(m => m.GetTypes()).FirstOrDefault(tp => tp.FullName == type);
        desc = desc.Replace(" ", string.Empty);
        return td?.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc));
    }

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

    public static void ReturnFalseMethod(this MethodDef method)
    {
        ReturnZeroMethod(method);
    }

    public static void ReturnTrueMethod(this MethodDef method)
    {
        ReturnOneMethod(method);
    }

    public static Instruction? FindInstruction(this MethodDef method, OpCode opCode, string operandName)
    {
        return method.Body.Instructions.FirstOrDefault(instruction =>
            instruction.OpCode == opCode && (instruction.Operand as IMethodDefOrRef)?.FullName == operandName);
    }

    public static void ReplaceWith(this MethodDef method, IEnumerable<Instruction> instructions)
    {
        method.Body.Instructions.Clear();
        foreach (var instruction in instructions)
        {
            method.Body.Instructions.Add(instruction);
        }
    }
}
