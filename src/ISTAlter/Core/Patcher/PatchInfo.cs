// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025-2026 TautCony

namespace ISTAlter.Core.Patcher;

using System.Reflection;
using dnlib.DotNet;
using ISTAlter.Core.Patcher.Provider;
using ISTAlter.Utils;

public class PatchInfo(Func<ModuleDefMD, int> delegator, MethodInfo method, int appliedCount)
{
    private int _appliedCount = appliedCount;
    private int _attemptedCount;

    public Func<ModuleDefMD, int> Delegator { get; set; } = delegator;

    public MethodInfo Method { get; } = method;

    public string[] LibraryNames { get; } = IPatcherProvider.ExtractLibrariesConfigFromAttribute(method);

    public Version? FromVersion { get; } = method.GetCustomAttribute<FromVersionAttribute>()?.Version;

    public Version? UntilVersion { get; } = method.GetCustomAttribute<UntilVersionAttribute>()?.Version;

    public int AppliedCount => this._appliedCount;

    /// <summary>Gets number of times the delegator was actually invoked (version and library checks passed).</summary>
    public int AttemptedCount => this._attemptedCount;

    public void AddAppliedCount(int count) => Interlocked.Add(ref this._appliedCount, count);

    public void AddAttemptedCount() => Interlocked.Increment(ref this._attemptedCount);
}
