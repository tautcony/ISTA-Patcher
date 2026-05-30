// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2026 TautCony

namespace ISTAlter.Core;

using Serilog;

public static partial class PatchUtils
{
    private static readonly AsyncLocal<string?> CurrentPatchName = new();

    /// <summary>
    /// Marks the currently executing patch so that any warning logged through
    /// <see cref="LogPatchWarning"/> while the scope is active is attributed to it.
    /// Set once by the dispatcher around each patch invocation.
    /// </summary>
    /// <param name="patchName">The name of the patch being executed.</param>
    /// <returns>A disposable that clears the attribution when the patch returns.</returns>
    public static IDisposable BeginPatchScope(string patchName)
    {
        CurrentPatchName.Value = patchName;
        return new PatchScope();
    }

    /// <summary>
    /// Logs a warning that is tied to a specific patch, prefixing it with the patch
    /// name captured by the active <see cref="BeginPatchScope"/>. Falls back to a plain
    /// warning when no patch scope is active.
    /// </summary>
    /// <param name="messageTemplate">The Serilog message template.</param>
    /// <param name="propertyValues">The values bound to the template placeholders.</param>
    public static void LogPatchWarning(string messageTemplate, params object?[] propertyValues)
    {
        var patchName = CurrentPatchName.Value;
        if (patchName == null)
        {
            Log.Warning(messageTemplate, propertyValues);
            return;
        }

        var values = new object?[propertyValues.Length + 1];
        values[0] = patchName;
        Array.Copy(propertyValues, 0, values, 1, propertyValues.Length);
        Log.Warning("[{PatchName}] " + messageTemplate, values);
    }

    private sealed class PatchScope : IDisposable
    {
        public void Dispose() => CurrentPatchName.Value = null;
    }
}
