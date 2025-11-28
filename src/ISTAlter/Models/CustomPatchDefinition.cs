// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Models;

/// <summary>
/// Represents a custom patch definition from configuration.
/// </summary>
public class CustomPatchDefinition
{
    /// <summary>
    /// Gets or sets the full name of the type containing the method to patch.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the method to patch.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature of the method in format: (param1Type,param2Type)returnType.
    /// Example: "(System.String,System.Boolean)System.Boolean".
    /// </summary>
    public string MethodSignature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type to apply to the method.
    /// Valid values: "ReturnTrue", "ReturnFalse", "ReturnZero", "ReturnOne", "Empty".
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific library file names this patch applies to.
    /// If empty, the patch will be applied to all matching assemblies.
    /// </summary>
    public string[]? LibraryNames { get; set; }

    /// <summary>
    /// Gets or sets a description of what this patch does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this patch is enabled.
    /// Default is true (always enabled).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum version requirement.
    /// </summary>
    public string? FromVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum version requirement (exclusive).
    /// </summary>
    public string? UntilVersion { get; set; }
}
