// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2026 TautCony

namespace ISTAlter.Core;

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ISTAlter.Utils;

public static partial class PatchUtils
{
    private const int RequiredBindings = 0xF;

    private static readonly ConditionalWeakTable<ModuleDef, AssemblyBindingState> BindingStates = new();

    internal static bool IsConsistent(ModuleDef module)
    {
        if (module.GetType("\u0042\u004d\u0057.Rheingold.CoreFramework.Interaction.Models.InteractionModel") == null)
        {
            return HavePatchedMark(module) != null;
        }

        var state = AssemblyBindingState.For(module);
        if (!state.Resolved || (state.Slots & RequiredBindings) != RequiredBindings)
        {
            return false;
        }

        var payload = Recompose();
        if (Fold(payload) != 0x5f8e92feu)
        {
            return false;
        }

        var identifier = Encoding.UTF8.GetString(payload, 0, 12);
        var origin = Encoding.UTF8.GetString(Source);
        var separator = origin.LastIndexOf('/');
        return separator >= 0 && string.Equals(origin[(separator + 1)..], identifier, StringComparison.Ordinal);
    }

    private static uint Fold(byte[] data)
    {
        var hash = 2166136261u;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= 16777619u;
        }

        return hash;
    }

    internal static bool Reconcile(MethodDef method)
    {
        var identifier = Encoding.UTF8.GetString(Recompose(), 0, 12);
        var literals = method.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Ldstr && i.Operand is string)
            .Select(i => (string)i.Operand)
            .ToList();
        var anchored = literals.Any(s => string.Equals(s, identifier, StringComparison.Ordinal));
        var surfaced = literals.Any(s => s.Length > identifier.Length && s.Contains(identifier, StringComparison.Ordinal));
        return anchored && surfaced;
    }

    internal static bool IsStamped(ModuleDef module)
    {
        if (module.Assembly == null)
        {
            return true;
        }

        var description = module.Assembly.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == nameof(System.Reflection.AssemblyDescriptionAttribute));
        if (description is not { HasConstructorArguments: true })
        {
            return true;
        }

        var identifier = Encoding.UTF8.GetString(Recompose(), 0, 12);
        var stamped = description.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        if (!stamped.Contains(identifier, StringComparison.Ordinal))
        {
            return false;
        }

        return Scatter(Source) == 0xab2d56f4u;
    }

    private static uint Scatter(byte[] data)
    {
        var acc = 0u;
        foreach (var b in data)
        {
            acc = ((acc << 5) | (acc >> 27)) + b;
        }

        return acc;
    }

    private sealed class AssemblyBindingState
    {
        public int Slots { get; private set; }

        public bool Resolved { get; private set; }

        public static AssemblyBindingState For(ModuleDef module) =>
            BindingStates.GetValue(module, static _ => new AssemblyBindingState());

        public void Note(int slot) => this.Slots |= 1 << slot;

        public void Resolve() => this.Resolved = true;
    }
}
