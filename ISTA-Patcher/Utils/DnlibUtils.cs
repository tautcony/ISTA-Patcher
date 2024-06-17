// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Utils;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

public static class DnlibUtils
{
    /// <summary>
    /// Retrieves the description of the specified <see cref="MethodDef"/>.
    /// </summary>
    /// <param name="md">The <see cref="MethodDef"/> to get the description of.</param>
    /// <returns>The description of the method as a string.</returns>
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
    /// Retrieves a <see cref="TypeDef"/> representing the specified type from the provided <see cref="ModuleDefMD"/>.
    /// </summary>
    /// <param name="module">The <see cref="ModuleDefMD"/> object representing the .NET assembly module to search for the type.</param>
    /// <param name="type">The full name of the type to retrieve, including the namespace and the type name (e.g., "Namespace.ClassName").</param>
    /// <returns>
    /// A <see cref="TypeDef"/> object representing the specified type if found; otherwise, null.
    /// </returns>
    public static TypeDef? GetType(this ModuleDefMD module, string type) =>
        module.GetTypes().FirstOrDefault(tp => string.Equals(tp.FullName, type, StringComparison.Ordinal));

    /// <summary>
    /// Retrieves a <see cref="MethodDef"/> from the specified <see cref="ModuleDefMD"/>.
    /// </summary>
    /// <param name="module">The <see cref="ModuleDefMD"/> to search for the method.</param>
    /// <param name="type">The full name of the type to retrieve, including the namespace and the type name.</param>
    /// <param name="name">The name of the method to retrieve.</param>
    /// <param name="desc">The description of the method.</param>
    /// <returns>
    /// A <see cref="MethodDef"/> object representing the specified method if found; otherwise, null.
    /// </returns>
    public static MethodDef? GetMethod(this ModuleDefMD module, string type, string name, string desc)
    {
        return module.GetType(type)?.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc, StringComparison.Ordinal));
    }

    /// <summary>
    /// Empties the given method.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> whose body will be emptied.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null.</exception>
    public static void EmptyingMethod(this MethodDef method)
    {
        var body = method.Body;
        if (body == null)
        {
            throw new ArgumentNullException($"{method.FullName}.Body null!");
        }

        body.Variables.Clear();
        body.ExceptionHandlers.Clear();
        body.Instructions.Clear();
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return zero.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> to modify.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null.</exception>
    public static void ReturnZeroMethod(this MethodDef method) => method.ReturningWithValue(0);

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return one.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> to modify.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null.</exception>
    public static void ReturnOneMethod(this MethodDef method) => method.ReturningWithValue(1);

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return one.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null.</exception>
    /// <returns>An action that modifies the body of the <see cref="MethodDef"/> to return the specified value.</returns>
    public static Action<MethodDef> ReturnUInt32Method(uint value)
    {
        return method => method.ReturningWithValue(value);
    }

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return false.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> to modify.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null.</exception>
    public static void ReturnFalseMethod(this MethodDef method) => method.ReturningWithValue(value: false);

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return true.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> to modify.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null.</exception>
    public static void ReturnTrueMethod(this MethodDef method) => method.ReturningWithValue(value: true);

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return the specified string.
    /// </summary>
    /// <param name="value">The string to return.</param>
    /// <returns>An action that modifies the body of the <see cref="MethodDef"/> to return the specified string.</returns>
    public static Action<MethodDef> ReturnStringMethod(string value)
    {
        return method => method.ReturningWithValue(value);
    }

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return the specified memberRef.
    /// </summary>
    /// <param name="value">The memberRef to return.</param>
    /// <returns>An action that modifies the body of the <see cref="MethodDef"/> to return the specified memberRef.</returns>
    public static Action<MethodDef> ReturnObjectMethod(MemberRef value)
    {
        return method => method.ReturningWithValue(value);
    }

    /// <summary>
    /// Finds the first instruction in the body of the <see cref="MethodDef"/> that matches the specified opcode and operand name.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> to search for the instruction.</param>
    /// <param name="opCode">The opcode of the instruction to find.</param>
    /// <param name="operandName">The full name of the operand to match.</param>
    /// <returns>The found <see cref="Instruction"/> or null if no matching instruction is found.</returns>
    public static Instruction? FindInstruction(this MethodDef method, OpCode opCode, string operandName)
    {
        return method.Body.Instructions.FirstOrDefault(instruction =>
            instruction.OpCode == opCode && string.Equals((instruction.Operand as IMethod)?.FullName, operandName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Finds the operand of the first instruction in the body of the <see cref="MethodDef"/> that matches the specified opcode and operand name.
    /// </summary>
    /// <typeparam name="T">The type of the operand to retrieve.</typeparam>
    /// <param name="method">The <see cref="MethodDef"/> to search for the operand.</param>
    /// <param name="opCode">The opcode of the instruction to find.</param>
    /// <param name="operandName">The full name of the operand to match.</param>
    /// <returns>The found operand of type <typeparamref name="T"/> or null if no matching instruction is found or the operand type is not compatible.</returns>
    public static T? FindOperand<T>(this MethodDef method, OpCode opCode, string operandName)
    {
        return (T)method.FindInstruction(opCode, operandName)?.Operand;
    }

    /// <summary>
    /// Replaces the instructions in the body of the <see cref="MethodDef"/> with the provided collection of instructions.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> whose instructions will be replaced.</param>
    /// <param name="instructions">The collection of instructions to replace the existing ones with.</param>
    public static void ReplaceWith(this MethodDef method, IEnumerable<Instruction> instructions)
    {
        method.Body.Instructions.Clear();
        foreach (var instruction in instructions)
        {
            method.Body.Instructions.Add(instruction);
        }
    }

    /// <summary>
    /// Builds a method call based on the provided parameters.
    /// </summary>
    /// <param name="module">The module definition.</param>
    /// <param name="type">The type containing the method.</param>
    /// <param name="method">The name of the method.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <param name="parameters">The array of parameter types.</param>
    /// <returns>An instance of the <see cref="IMethod"/> interface representing the method call, or null if the method was not found.</returns>
    [Obsolete("Importer may include unwanted dependencies.")]
    public static IMethod? BuildCall(
        ModuleDef module,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type type,
        string method,
        Type returnType,
        Type[]? parameters)
    {
        var importer = new Importer(module);

        MethodBase mb;
        if (string.Equals(method, ".ctor", StringComparison.Ordinal))
        {
            mb = Array.Find(type.GetConstructors(), m => CheckParametersByType(m, parameters));
        }
        else
        {
            mb = Array.Find(type.GetMethods(), m => string.Equals(m.Name, method, StringComparison.Ordinal) && m.ReturnType == returnType && CheckParametersByType(m, parameters));
        }

        return importer.Import(mb);
    }

    /// <summary>
    /// Modifies the body of the <see cref="MethodDef"/> to return a specific value.
    /// </summary>
    /// <param name="method">The <see cref="MethodDef"/> to modify.</param>
    /// <param name="value">The value to return.</param>
    /// <exception cref="ArgumentNullException">Thrown if the body of the method is null or if the type of the value is not supported.</exception>
    private static void ReturningWithValue<T>(this MethodDef method, T? value)
    {
        var body = method.Body;
        if (body == null)
        {
            throw new ArgumentNullException($"{method.FullName}.Body null!");
        }

        body.Variables.Clear();
        body.ExceptionHandlers.Clear();
        body.Instructions.Clear();
        switch (value)
        {
            case byte b:
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, b));
                break;
            case int i:
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                break;
            case uint i:
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)i));
                break;
            case long l:
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I8, l));
                break;
            case bool b:
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, b ? 1 : 0));
                break;
            case string s:
                body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, s));
                break;
            case MemberRef m:
                body.Instructions.Add(Instruction.Create(OpCodes.Newobj, m));
                break;
            default:
                if (value != null)
                {
                    throw new ArgumentException($"Unknown type {value.GetType().FullName}!", paramName: nameof(value));
                }

                break;
        }

        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    /// <summary>
    /// Checks whether the parameters of a given method match the specified types.
    /// </summary>
    /// <param name="method">The <see cref="MethodBase"/> representing the method to check.</param>
    /// <param name="types">The collection of parameter types to compare against.</param>
    /// <returns><see langword="true"/> if the method's parameters match the specified types; otherwise, <see langword="false"/>.</returns>
    private static bool CheckParametersByType(MethodBase method, IReadOnlyCollection<Type>? types)
    {
        if ((types?.Count ?? 0) == method.GetParameters().Length)
        {
            return types == null || method.GetParameters().Zip(types, (first, second) => first.ParameterType == second).All(b => b);
        }

        return false;
    }

    /// <summary>
    /// Recursively searches for a property in the given <paramref name="targetType"/> and its base classes.
    /// </summary>
    /// <param name="targetType">The <see cref="TypeDef"/> to start the search from.</param>
    /// <param name="propertyName">The name of the property to find.</param>
    /// <returns>
    /// The <see cref="PropertyDef"/> representing the property found, or <see langword="null"/> if the property was not found.
    /// </returns>
    public static PropertyDef? FindPropertyInClassAndBaseClasses(TypeDef targetType, string propertyName)
    {
        while (targetType != null)
        {
            var targetProperty = targetType.FindProperty(propertyName);
            if (targetProperty != null)
            {
                return targetProperty;
            }

            targetType = targetType.BaseType?.ResolveTypeDef();
        }

        return null;
    }

    public static Local? GetLocalByType(this MethodDef method, string fullTypeName)
    {
        return method.Body.Variables.FirstOrDefault(variable => string.Equals(variable.Type.FullName, fullTypeName, StringComparison.Ordinal));
    }
}
