// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Models;

using System.Reflection;

public sealed class ParameterDescriptor
{
    public required string Name { get; init; }

    public required string DisplayName { get; init; }

    public string Description { get; init; } = string.Empty;

    public required ParameterKind Kind { get; init; }

    public required Type PropertyType { get; init; }

    public bool IsRequired { get; init; }

    public bool IsArgument { get; init; }

    public bool IsParentOption { get; init; }

    public object? DefaultValue { get; init; }

    public string[] EnumValues { get; init; } = [];

    public required PropertyInfo PropertyInfo { get; init; }
}
