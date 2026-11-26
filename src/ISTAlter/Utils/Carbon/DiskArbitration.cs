// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAlter.Utils.Carbon;

using System.Runtime.InteropServices;
#pragma warning disable SA1135
using CFURLRef = nint;
using DADiskRef = nint;
using DASessionRef = nint;
#pragma warning restore SA1135

internal static partial class DiskArbitration
{
    private const string DiskArbitrationLibrary = "/System/Library/Frameworks/DiskArbitration.framework/DiskArbitration";

    /// <summary>
    /// Creates a new session.
    /// </summary>
    /// <param name="allocator">The allocator object to be used to allocate memory.</param>
    /// <returns>A reference to a new DASession.</returns>
    [LibraryImport(DiskArbitrationLibrary)]
    internal static partial SafeCreateHandle DASessionCreate(nint allocator);

    /// <summary>
    /// Creates a new disk object.
    /// </summary>
    /// <param name="allocator">The allocator object to be used to allocate memory.</param>
    /// <param name="session">The DASession in which to contact Disk Arbitration.</param>
    /// <param name="path">The BSD mount point.</param>
    /// <returns>A reference to a new DADisk.</returns>
    [LibraryImport(DiskArbitrationLibrary)]
    internal static partial SafeCreateHandle DADiskCreateFromVolumePath(nint allocator, DASessionRef session, CFURLRef path);

    /// <summary>
    /// Obtains the Disk Arbitration description of the specified disk.
    /// </summary>
    /// <param name="disk">The DADisk for which to obtain the Disk Arbitration description.</param>
    /// <returns>The disk’s Disk Arbitration description.</returns>
    [LibraryImport(DiskArbitrationLibrary)]
    internal static partial SafeCreateHandle DADiskCopyDescription(DADiskRef disk);
}
