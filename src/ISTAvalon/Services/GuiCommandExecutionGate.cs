// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

/// <summary>
/// Process-wide single-flight gate for GUI command execution so tabs cannot
/// overlap DelegateLogSink subscription or Console capture scopes.
/// </summary>
public static class GuiCommandExecutionGate
{
    private static int _busy;

    public static event Action? StateChanged;

    public static bool IsBusy => Volatile.Read(ref _busy) != 0;

    public static bool TryEnter()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            return false;
        }

        StateChanged?.Invoke();
        return true;
    }

    public static void Exit()
    {
        if (Interlocked.Exchange(ref _busy, 0) != 0)
        {
            StateChanged?.Invoke();
        }
    }
}
