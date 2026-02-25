// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Models;

public sealed class CommandDescriptor
{
    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public required Type CommandType { get; init; }

    public Type? ParentCommandType { get; init; }

    public bool IsHidden { get; init; }

    public required IReadOnlyList<ParameterDescriptor> Parameters { get; init; }

    public IReadOnlyList<CommandDescriptor> Subcommands { get; init; } = [];
}
