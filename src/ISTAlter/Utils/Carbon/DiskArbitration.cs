// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Utils.Carbon;

using System.Runtime.InteropServices;
using CFURLRef = System.IntPtr;
using DADiskRef = System.IntPtr;
using DASessionRef = System.IntPtr;

internal partial class DiskArbitration
{
    private DiskArbitration()
    {
    }

    private const string DiskArbitrationLibrary = "/System/Library/Frameworks/DiskArbitration.framework/DiskArbitration";

    /// <summary>
    /// Creates a new session.
    /// </summary>
    /// <param name="allocator">The allocator object to be used to allocate memory.</param>
    /// <returns>A reference to a new DASession.</returns>
    [LibraryImport(DiskArbitrationLibrary)]
    internal static partial SafeCreateHandle DASessionCreate(IntPtr allocator);

    /// <summary>
    /// Creates a new disk object.
    /// </summary>
    /// <param name="allocator">The allocator object to be used to allocate memory.</param>
    /// <param name="session">The DASession in which to contact Disk Arbitration.</param>
    /// <param name="path">The BSD mount point.</param>
    /// <returns>A reference to a new DADisk.</returns>
    [LibraryImport(DiskArbitrationLibrary)]
    internal static partial SafeCreateHandle DADiskCreateFromVolumePath(IntPtr allocator, DASessionRef session, CFURLRef path);

    /// <summary>
    /// Obtains the Disk Arbitration description of the specified disk.
    /// </summary>
    /// <param name="disk">The DADisk for which to obtain the Disk Arbitration description.</param>
    /// <returns>The disk’s Disk Arbitration description.</returns>
    [LibraryImport(DiskArbitrationLibrary)]
    internal static partial SafeCreateHandle DADiskCopyDescription(DADiskRef disk);
}
