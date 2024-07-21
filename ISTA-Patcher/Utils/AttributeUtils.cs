// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Utils;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class ISTAPatchAttribute : Attribute;

internal sealed class ValidationPatchAttribute : ISTAPatchAttribute;

internal sealed class EssentialPatchAttribute : ISTAPatchAttribute;

internal sealed class SignaturePatchAttribute : ISTAPatchAttribute;

internal sealed class ToyotaPatchAttribute : ISTAPatchAttribute;

internal sealed class ENETPatchAttribute : ISTAPatchAttribute;

internal sealed class RequirementsPatchAttribute : ISTAPatchAttribute;

internal sealed class NotSendPatchAttribute : ISTAPatchAttribute;

internal sealed class UserAuthPatchAttribute : ISTAPatchAttribute;

internal sealed class SkipSyncClientConfigAttribute : ISTAPatchAttribute;

internal sealed class MarketLanguagePatchAttribute : ISTAPatchAttribute;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class FromVersionAttribute(string version) : Attribute
{
    public string? Version { get; set; } = version;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class LibraryNameAttribute(params string[] fileName) : Attribute
{
    public string[] FileName { get; set; } = fileName;
}
