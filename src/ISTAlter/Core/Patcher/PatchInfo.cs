// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025-2026 TautCony

namespace ISTAlter.Core.Patcher;

using System.Reflection;
using dnlib.DotNet;
using ISTAlter.Core.Patcher.Provider;
using ISTAlter.Utils;

public class PatchInfo
{
    private int _appliedCount;
    private int _attemptedCount;

    public PatchInfo(Func<ModuleDefMD, int> delegator, MethodInfo method, int appliedCount)
    {
        this.Delegator = delegator;
        this.Method = method;
        this._appliedCount = appliedCount;
        this.LibraryNames = IPatcherProvider.ExtractLibrariesConfigFromAttribute(method);
        this.FromVersion = method.GetCustomAttribute<FromVersionAttribute>()?.Version;
        this.UntilVersion = method.GetCustomAttribute<UntilVersionAttribute>()?.Version;
    }

    public Func<ModuleDefMD, int> Delegator { get; set; }

    public MethodInfo Method { get; }

    public string[] LibraryNames { get; }

    public Version? FromVersion { get; }

    public Version? UntilVersion { get; }

    public int AppliedCount => this._appliedCount;

    /// <summary>Gets number of times the delegator was actually invoked (version and library checks passed).</summary>
    public int AttemptedCount => this._attemptedCount;

    public void AddAppliedCount(int count) => Interlocked.Add(ref this._appliedCount, count);

    public void AddAttemptedCount() => Interlocked.Increment(ref this._attemptedCount);
}
