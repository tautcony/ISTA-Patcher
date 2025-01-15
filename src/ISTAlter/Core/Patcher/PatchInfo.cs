// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Core.Patcher;

using System.Reflection;
using dnlib.DotNet;

public class PatchInfo(Func<ModuleDefMD, int> delegator, MethodInfo method, int appliedCount)
{
    public Func<ModuleDefMD, int> Delegator { get; set; } = delegator;

    public MethodInfo Method { get; set; } = method;

    public int AppliedCount { get; set; } = appliedCount;
}
