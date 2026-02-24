// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAlter.Utils;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ISTAlter.Utils.Carbon.CoreFoundation;
using static ISTAlter.Utils.Carbon.DiskArbitration;
using static ISTAlter.Utils.Carbon.IOKit;

public static partial class NativeMethods
{
    /// <summary>
    /// Retrieves the Machine UUID.
    /// </summary>
    /// <returns>The Machine UUID, or an empty string if not found.</returns>
    [SupportedOSPlatform("macOS")]
    public static string GetMacVolumeSerialNumber()
    {
        var ret = string.Empty;

        using var session = DASessionCreate(IntPtr.Zero);
        var urlBytes = "/"u8.ToArray();
        using var url = CFURLCreateFromFileSystemRepresentation(IntPtr.Zero, urlBytes, urlBytes.Length, isDirectory: true);
        using var disk = DADiskCreateFromVolumePath(IntPtr.Zero, session.DangerousGetHandle(), url.DangerousGetHandle());
        if (disk.IsInvalid)
        {
            return ret;
        }

        using var description = DADiskCopyDescription(disk.DangerousGetHandle());
        if (description.IsInvalid)
        {
            return ret;
        }

        using var DAVolumeUUIDKey = CFStringCreateWithCString(IntPtr.Zero, "DAVolumeUUID", CFStringBuiltInEncodings.kCFStringEncodingUTF8);
        var valuePtr = CFDictionaryGetValue(description.DangerousGetHandle(), DAVolumeUUIDKey.DangerousGetHandle());
        if (valuePtr == IntPtr.Zero)
        {
            return ret;
        }

        using var uuidCFString = CFUUIDCreateString(IntPtr.Zero, valuePtr);
        var interiorPointer = CFStringGetCStringPtr(uuidCFString.DangerousGetHandle(), CFStringBuiltInEncodings.kCFStringEncodingUTF8);
        if (interiorPointer != IntPtr.Zero)
        {
            ret = Marshal.PtrToStringUTF8(interiorPointer)!;
        }

        return ret;
    }

    /// <summary>
    /// Retrieves the IOPlatformUUID.
    /// </summary>
    /// <returns>The IOPlatformUUID, or an empty string if not found.</returns>
    [SupportedOSPlatform("macOS")]
    public static string GetIOPlatformUUID()
    {
        var matchingDict = IOServiceMatching("IOPlatformExpertDevice");
        var service = IOServiceGetMatchingService(IntPtr.Zero, matchingDict);
        try
        {
            using var uuidKey = CFStringCreateWithCString(IntPtr.Zero, "IOPlatformUUID", CFStringBuiltInEncodings.kCFStringEncodingUTF8);
            using var uuidPtr = IORegistryEntryCreateCFProperty(service, uuidKey.DangerousGetHandle(), IntPtr.Zero, 0);

            var ret = string.Empty;
            var interiorPointer = CFStringGetCStringPtr(uuidPtr.DangerousGetHandle(), CFStringBuiltInEncodings.kCFStringEncodingUTF8);
            if (interiorPointer != IntPtr.Zero)
            {
                ret = Marshal.PtrToStringUTF8(interiorPointer)!;
            }

            return ret;
        }
        finally
        {
            if (service != IntPtr.Zero)
            {
                IOObjectRelease(service);
            }
        }
    }
}
