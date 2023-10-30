// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.Utils;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class ISTAPatch : Attribute
{
}

internal class ValidationPatch : ISTAPatch
{
}

internal class EssentialPatch : ISTAPatch
{
}

internal class SignaturePatch : ISTAPatch
{
}

internal class ToyotaPatch : ISTAPatch
{
}

internal class ENETPatch : ISTAPatch
{
}

internal class RequirementsPatch : ISTAPatch
{
}

internal class NotSendPatch : ISTAPatch
{
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class FromVersion : Attribute
{
    public string? Version { get; set; }
}
