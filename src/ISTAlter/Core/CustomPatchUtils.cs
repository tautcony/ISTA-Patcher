// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Core;

using System.Reflection;
using dnlib.DotNet;
using ISTAlter.Core.Patcher;
using ISTAlter.Models;
using ISTAlter.Utils;
using Serilog;

/// <summary>
/// Utility class for applying custom patches defined in configuration.
/// </summary>
public static class CustomPatchUtils
{
    /// <summary>
    /// Gets the operation delegate based on the operation type string.
    /// </summary>
    /// <param name="operationType">The operation type string.</param>
    /// <returns>An Action delegate that performs the specified operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the operation type is not supported.</exception>
    public static Action<MethodDef> GetOperation(string operationType)
    {
        return operationType.ToLowerInvariant() switch
        {
            "returntrue" => DnlibUtils.ReturnTrueMethod,
            "returnfalse" => DnlibUtils.ReturnFalseMethod,
            "returnzero" => DnlibUtils.ReturnZeroMethod,
            "returnone" => DnlibUtils.ReturnOneMethod,
            "empty" or "emptying" => DnlibUtils.EmptyingMethod,
            _ => throw new ArgumentException($"Unsupported operation type: {operationType}", nameof(operationType)),
        };
    }

    /// <summary>
    /// Creates a patch method for a custom patch definition.
    /// </summary>
    private static (Func<ModuleDefMD, int> PatchFunc, MethodInfo MethodInfo) CreatePatchMethod(CustomPatchDefinition definition)
    {
        // Create a lambda that will be invoked by PatchSingleFile
        Func<ModuleDefMD, int> patchFunc = module =>
        {
            try
            {
                var operation = GetOperation(definition.OperationType);
                var result = module.PatchFunction(
                    definition.TypeName,
                    definition.MethodName,
                    definition.MethodSignature,
                    operation
                );

                if (result > 0)
                {
                    Log.Debug(
                        "Custom patch applied: {TypeName}.{MethodName} with {Operation}",
                        definition.TypeName,
                        definition.MethodName,
                        definition.OperationType
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed to apply custom patch: {TypeName}.{MethodName}",
                    definition.TypeName,
                    definition.MethodName
                );
                return 0;
            }
        };

        // Create a wrapper MethodInfo with custom attributes and custom name
        // Add "Patch" prefix to match the naming convention used by built-in patches
        var wrapperMethod = CreateMethodInfoWithAttributes(patchFunc.Method, definition, $"PatchFromConfig_{definition.MethodName}");

        return (patchFunc, wrapperMethod);
    }

    /// <summary>
    /// Creates a MethodInfo wrapper with custom attributes based on the patch definition.
    /// </summary>
    private static MethodInfoWrapper CreateMethodInfoWithAttributes(MethodInfo baseMethod, CustomPatchDefinition definition, string customName)
    {
        var attributes = new List<Attribute>();

        // Add LibraryNameAttribute if specified
        if (definition.LibraryNames is { Length: > 0 })
        {
            attributes.Add(new LibraryNameAttribute(definition.LibraryNames));
        }

        // Add FromVersionAttribute if specified
        if (!string.IsNullOrWhiteSpace(definition.FromVersion))
        {
            try
            {
                attributes.Add(new FromVersionAttribute(definition.FromVersion));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse FromVersion for {MethodName}: {Version}", definition.MethodName, definition.FromVersion);
            }
        }

        // Add UntilVersionAttribute if specified
        if (!string.IsNullOrWhiteSpace(definition.UntilVersion))
        {
            try
            {
                attributes.Add(new UntilVersionAttribute(definition.UntilVersion));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse UntilVersion for {MethodName}: {Version}", definition.MethodName, definition.UntilVersion);
            }
        }

        // Return a wrapped MethodInfo with attributes and custom name
        return new MethodInfoWrapper(baseMethod, attributes.ToArray(), customName);
    }

    /// <summary>
    /// Creates PatchInfo instances from custom patch definitions.
    /// </summary>
    /// <param name="definitions">The custom patch definitions.</param>
    /// <returns>A list of PatchInfo instances.</returns>
    public static List<PatchInfo> CreatePatchInfoFromDefinitions(IEnumerable<CustomPatchDefinition> definitions)
    {
        var patchInfoList = new List<PatchInfo>();

        foreach (var definition in definitions)
        {
            if (!definition.Enabled)
            {
                continue;
            }

            var (patchFunc, methodInfo) = CreatePatchMethod(definition);
            patchInfoList.Add(new PatchInfo(patchFunc, methodInfo, 0));
        }

        return patchInfoList;
    }
}
