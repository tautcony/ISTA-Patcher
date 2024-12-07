// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Utils;

using System.Runtime.Versioning;
using System.Text;
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

        var session = DASessionCreate(IntPtr.Zero);
        var urlBytes = "/"u8.ToArray();
        var url = CFURLCreateFromFileSystemRepresentation(IntPtr.Zero, urlBytes, urlBytes.Length, true);
        var disk = DADiskCreateFromVolumePath(IntPtr.Zero, session, url);
        if (disk != IntPtr.Zero)
        {
            var description = DADiskCopyDescription(disk);
            if (description != IntPtr.Zero)
            {
                var DAVolumeUUIDKey = CFStringCreateWithCString(IntPtr.Zero, "DAVolumeUUID"u8.ToArray(), CFStringBuiltInEncodings.kCFStringEncodingUTF8);
                var valuePtr = CFDictionaryGetValue(description, DAVolumeUUIDKey);
                if (valuePtr != IntPtr.Zero)
                {
                    var uuidCFString = CFUUIDCreateString(IntPtr.Zero, valuePtr);
                    var uuidBuffer = new byte[128];

                    if (CFStringGetCString(uuidCFString, uuidBuffer, uuidBuffer.Length, CFStringBuiltInEncodings.kCFStringEncodingUTF8))
                    {
                        ret = Encoding.UTF8.GetString(uuidBuffer);
                    }

                    CFRelease(uuidCFString);
                }

                CFRelease(DAVolumeUUIDKey);
                CFRelease(description);
                CFRelease(disk);
            }
        }

        CFRelease(url);
        CFRelease(session);
        return ret;
    }

    /// <summary>
    /// Retrieves the IOPlatformUUID.
    /// </summary>
    /// <returns>The IOPlatformUUID, or an empty string if not found.</returns>
    [SupportedOSPlatform("macOS")]
    public static string GetIOPlatformUUID()
    {
        var matchingDict = IOServiceMatching("IOPlatformExpertDevice"u8.ToArray());
        var service = IOServiceGetMatchingService(IntPtr.Zero, matchingDict);
        var uuidKey = CFStringCreateWithCString(IntPtr.Zero, "IOPlatformUUID"u8.ToArray(), CFStringBuiltInEncodings.kCFStringEncodingUTF8);
        var uuidPtr = IORegistryEntryCreateCFProperty(service, uuidKey, IntPtr.Zero, 0);

        var uuid = new byte[128];
        var ret = string.Empty;
        if (CFStringGetCString(uuidPtr, uuid, uuid.Length, CFStringBuiltInEncodings.kCFStringEncodingUTF8))
        {
            ret = Encoding.UTF8.GetString(uuid);
        }

        CFRelease(uuidPtr);
        CFRelease(uuidKey);

        return ret;
    }
}
