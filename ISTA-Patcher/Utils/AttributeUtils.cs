// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Utils;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class ISTAPatch : Attribute;

internal sealed class ValidationPatch : ISTAPatch;

internal sealed class EssentialPatch : ISTAPatch;

internal sealed class SignaturePatch : ISTAPatch;

internal sealed class ToyotaPatch : ISTAPatch;

internal sealed class ENETPatch : ISTAPatch;

internal sealed class RequirementsPatch : ISTAPatch;

internal sealed class NotSendPatch : ISTAPatch;

internal sealed class UserAuthPatch : ISTAPatch;

internal sealed class LogEnviromentPatch : ISTAPatch;

internal sealed class SkipSyncClientConfig : ISTAPatch;

internal sealed class MarketLanguagePatch : ISTAPatch;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class FromVersion(string version) : Attribute
{
    public string? Version { get; set; } = version;
}
