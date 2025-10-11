// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAlter.Utils.Carbon;

using System.Runtime.InteropServices;
using Serilog;
using CFBooleanRef = System.IntPtr;
using CFDictionaryRef = System.IntPtr;
using CFNumberRef = System.IntPtr;
using CFStringRef = System.IntPtr;
using CFURLRef = System.IntPtr;
using CFUUIDRef = System.IntPtr;

internal static partial class CoreFoundation
{
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    /// <summary>
    /// Releases a Core Foundation object.
    /// </summary>
    /// <param name="cf">A CFType object to release. This value must not be NULL.</param>
    [LibraryImport(CoreFoundationLibrary)]
    internal static partial void CFRelease(IntPtr cf);

    /// <summary>
    /// Creates an immutable string from a C string.
    /// </summary>
    /// <param name="allocator">The allocator to use to allocate memory for the new string. Pass NULL or kCFAllocatorDefault to use the current default allocator.</param>
    /// <param name="cStr">The NULL-terminated C string to be used to create the CFString object. The string must use an 8-bit encoding.</param>
    /// <param name="encoding">The encoding of the characters in the C string. The encoding must specify an 8-bit encoding.</param>
    /// <returns>An immutable string containing cStr (after stripping off the NULL terminating character), or NULL if there was a problem creating the object. Ownership follows the The Create Rule.</returns>
    [LibraryImport(CoreFoundationLibrary, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial SafeCreateHandle CFStringCreateWithCString(IntPtr allocator, string cStr, CFStringBuiltInEncodings encoding);

    /// <summary>
    /// Quickly obtains a pointer to a C-string buffer containing the characters of a string in a given encoding.
    /// </summary>
    /// <param name="theString">The string whose contents you wish to access.</param>
    /// <param name="encoding">The string encoding to which the character contents of theString should be converted. The encoding must specify an 8-bit encoding.</param>
    /// <returns>A pointer to a C string or NULL if the internal storage of theString does not allow this to be returned efficiently.</returns>
    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFStringGetCStringPtr(CFStringRef theString, CFStringBuiltInEncodings encoding);

    /// <summary>
    /// Returns the value associated with a given key.
    /// </summary>
    /// <param name="theDict">The dictionary to examine.</param>
    /// <param name="key">The key for which to find a match in theDict. The key hash and equal callbacks provided when the dictionary was created are used to compare. If the hash callback was NULL, the key is treated as a pointer and converted to an integer. If the equal callback was NULL, pointer equality (in C, ==) is used. If key, or any of the keys in theDict, is not understood by the equal callback, the behavior is undefined.</param>
    /// <returns>The value associated with key in theDict, or NULL if no key-value pair matching key exists. Since NULL is also a valid value in some dictionaries, use CFDictionaryGetValueIfPresent to distinguish between a value that is not found, and a NULL value. If the value is a Core Foundation object, ownership follows the The Get Rule.</returns>
    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFDictionaryGetValue(CFDictionaryRef theDict, CFStringRef key);

    /// <summary>
    /// Returns the string representation of a specified CFUUID object.
    /// </summary>
    /// <param name="allocator">The allocator to use to allocate memory for the new string. Pass NULL or kCFAllocatorDefault to use the current default allocator.</param>
    /// <param name="uuid">The CFUUID object whose string representation to obtain.</param>
    /// <returns>The string representation of uuid. Ownership follows the The Create Rule.</returns>
    [LibraryImport(CoreFoundationLibrary)]
    internal static partial SafeCreateHandle CFUUIDCreateString(IntPtr allocator, CFUUIDRef uuid);

    /// <summary>
    /// Creates a new CFURL object for a file system entity using the native representation.
    /// </summary>
    /// <param name="allocator">The allocator to use to allocate memory for the new CFURL object. Pass NULL or kCFAllocatorDefault to use the current default allocator.</param>
    /// <param name="buffer">The character bytes to convert into a CFURL object. This should be the path as you would use in POSIX function calls.</param>
    /// <param name="bufLen">The number of character bytes in the buffer (usually the result of a call to strlen), not including any null termination.</param>
    /// <param name="isDirectory">A Boolean value that specifies whether the string is treated as a directory path when resolving against relative path components—true if the pathname indicates a directory, false otherwise.</param>
    /// <returns></returns>
    [LibraryImport(CoreFoundationLibrary)]
    internal static partial SafeCreateHandle CFURLCreateFromFileSystemRepresentation(IntPtr allocator, [In] byte[] buffer, long bufLen, [MarshalAs(UnmanagedType.U1)] bool isDirectory);

    /// <summary>
    /// Flags used by CFNumber to indicate the data type of a value.
    /// </summary>
    internal enum CFNumberType
    {
        CFNumberCharType = 7,
        kCFNumberShortType = 8,
        kCFNumberIntType = 9,
        kCFNumberLongType = 10,
    }

    /// <summary>
    /// Encodings that are built-in on all platforms on which macOS runs.
    /// </summary>
    internal enum CFStringBuiltInEncodings
    {
        kCFStringEncodingMacRoman = 0,
        kCFStringEncodingWindowsLatin1 = 0x0500,
        kCFStringEncodingISOLatin1 = 0x0201,
        kCFStringEncodingNextStepLatin = 0x0B01,
        kCFStringEncodingASCII = 0x0600,
        kCFStringEncodingUnicode = 0x0100,
        kCFStringEncodingUTF8 = 0x08000100,
        kCFStringEncodingNonLossyASCII = 0x0BFF,
        kCFStringEncodingUTF16 = kCFStringEncodingUnicode,
        kCFStringEncodingUTF16BE = 0x10000100,
        kCFStringEncodingUTF16LE = 0x14000100,
        kCFStringEncodingUTF32 = 0x0c000100,
        kCFStringEncodingUTF32BE = 0x18000100,
        kCFStringEncodingUTF32LE = 0x1c000100,
    }

    // for debug
    [LibraryImport(CoreFoundationLibrary)]
    internal static partial int CFDictionaryGetCount(CFDictionaryRef theDict);

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial void CFDictionaryGetKeysAndValues(CFDictionaryRef theDict, [Out] IntPtr[] keys, [Out] IntPtr[] values);

    [LibraryImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFNumberGetValue(CFNumberRef number, CFNumberType theType, out int value);

    [LibraryImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFBooleanGetValue(CFBooleanRef boolean);

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial CFStringRef CFURLGetString(CFURLRef anURL);

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFGetTypeID(IntPtr cf);

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFStringGetTypeID();

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFNumberGetTypeID();

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFBooleanGetTypeID();

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFUUIDGetTypeID();

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFArrayGetTypeID();

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFDictionaryGetTypeID();

    [LibraryImport(CoreFoundationLibrary)]
    internal static partial IntPtr CFURLGetTypeID();

    /// <summary>
    /// Prints the contents of a CFDictionary to the log.
    /// </summary>
    /// <param name="dict">The CFDictionary to print.</param>
    internal static void PrintDict(CFDictionaryRef dict)
    {
        var count = CFDictionaryGetCount(dict);
        var keys = new IntPtr[count];
        var values = new IntPtr[count];
        CFDictionaryGetKeysAndValues(dict, keys, values);
        for (var i = 0; i < count; i++)
        {
            var key = keys[i];
            var value = values[i];

            var keyInteriorPointer = CFStringGetCStringPtr(key, CFStringBuiltInEncodings.kCFStringEncodingUTF8);
            if (keyInteriorPointer == IntPtr.Zero)
            {
                continue;
            }

            var keyString = Marshal.PtrToStringUTF8(keyInteriorPointer)!;
            var typeId = CFGetTypeID(value);

            if (typeId == CFStringGetTypeID())
            {
                var valueInteriorPointer = CFStringGetCStringPtr(value, CFStringBuiltInEncodings.kCFStringEncodingUTF8);
                var valueString = valueInteriorPointer != IntPtr.Zero ? Marshal.PtrToStringUTF8(valueInteriorPointer)! : "<null string>";
                Log.Information("{KeyString}: {ValueString}", keyString, valueString);
            }
            else if (typeId == CFNumberGetTypeID())
            {
                if (CFNumberGetValue(value, CFNumberType.kCFNumberIntType, out var intValue))
                {
                    Log.Information("{KeyString}: {IntValue}", keyString, intValue);
                }
            }
            else if (typeId == CFBooleanGetTypeID())
            {
                var boolValue = CFBooleanGetValue(value);
                Log.Information("{KeyString}: {BoolValue}", keyString, boolValue);
            }
            else if (typeId == CFUUIDGetTypeID())
            {
                using var uuidCFString = CFUUIDCreateString(IntPtr.Zero, value);
                var valueInteriorPointer = CFStringGetCStringPtr(uuidCFString.DangerousGetHandle(), CFStringBuiltInEncodings.kCFStringEncodingUTF8);
                var uuidString = valueInteriorPointer != IntPtr.Zero ? Marshal.PtrToStringUTF8(valueInteriorPointer)! : "<null uuid>";
                Log.Information("{KeyString}: {UuidString}", keyString, uuidString);
            }
            else if (typeId == CFArrayGetTypeID())
            {
                Log.Information("{KeyString}: <array>", keyString);
            }
            else if (typeId == CFDictionaryGetTypeID())
            {
                Log.Information("{KeyString}: <dictionary>", keyString);
            }
            else if (typeId == CFURLGetTypeID())
            {
                var urlString = CFURLGetString(value);
                var urlInteriorPointer = CFStringGetCStringPtr(urlString, CFStringBuiltInEncodings.kCFStringEncodingUTF8);
                var url = urlInteriorPointer != IntPtr.Zero ? Marshal.PtrToStringUTF8(urlInteriorPointer)! : "<null url>";
                Log.Information("{KeyString}: {Url}", keyString, url);
            }
            else
            {
                Log.Information("{KeyString}: <unknown type>, type ID: {TypeId}", keyString, typeId);
            }
        }
    }
}
