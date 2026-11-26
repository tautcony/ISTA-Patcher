// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAlter.Utils.Carbon;

using System.Runtime.InteropServices;

internal sealed class SafeCreateHandle : SafeHandle
{
    public SafeCreateHandle()
        : base(nint.Zero, ownsHandle: true)
    {
    }

    internal SafeCreateHandle(nint ptr)
        : base(nint.Zero, ownsHandle: true)
    {
        this.SetHandle(ptr);
    }

    protected override bool ReleaseHandle()
    {
        CoreFoundation.CFRelease(this.handle);

        return true;
    }

    public override bool IsInvalid => this.handle == nint.Zero;
}
