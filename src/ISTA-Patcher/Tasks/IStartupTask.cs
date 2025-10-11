// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Tasks;

public interface IStartupTask
{
    void Execute(object?[]? parameters);
}
