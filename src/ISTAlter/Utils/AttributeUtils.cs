// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

namespace ISTAlter.Utils;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class ISTAPatchAttribute : Attribute;

internal sealed class ValidationPatchAttribute : ISTAPatchAttribute;

internal sealed class EssentialPatchAttribute : ISTAPatchAttribute;

internal sealed class SignaturePatchAttribute : ISTAPatchAttribute;

internal sealed class ToyotaPatchAttribute : ISTAPatchAttribute;

internal sealed class ENETPatchAttribute : ISTAPatchAttribute;

internal sealed class FinishedOPPatchAttribute : ISTAPatchAttribute;

internal sealed class RequirementsPatchAttribute : ISTAPatchAttribute;

internal sealed class NotSendPatchAttribute : ISTAPatchAttribute;

internal sealed class UserAuthPatchAttribute : ISTAPatchAttribute;

internal sealed class SyncClientConfigAttribute : ISTAPatchAttribute;

internal sealed class MarketLanguagePatchAttribute : ISTAPatchAttribute;

internal sealed class EnableOfflinePatchAttribute : ISTAPatchAttribute;

internal sealed class DisableFakeFSCRejectPatchAttribute : ISTAPatchAttribute;

internal sealed class EnableAirClientPatchAttribute : ISTAPatchAttribute;

internal sealed class DisableBrandCompatibleCheckPatchAttribute : ISTAPatchAttribute;

internal sealed class FixDS2VehicleIdentificationPatchAttribute : ISTAPatchAttribute;

internal sealed class ForceICOMNextPatchAttribute : ISTAPatchAttribute;

internal sealed class MotorbikeClamp15PatchAttribute : ISTAPatchAttribute;

internal sealed class ManualClampSwitchPatchAttribute : ISTAPatchAttribute;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class FromVersionAttribute(string version) : Attribute
{
    public Version? Version { get; } = new(version);
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class UntilVersionAttribute(string version) : Attribute
{
    public Version? Version { get; } = new(version);
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class LibraryNameAttribute(params string[] fileName) : Attribute
{
    public string[] FileName { get; } = fileName;
}
