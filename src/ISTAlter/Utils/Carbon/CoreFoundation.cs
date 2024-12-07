// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Utils.Carbon;

using System.Runtime.InteropServices;
using System.Text;
using Serilog;

internal partial class CoreFoundation
{
    /// <summary>
    /// Releases a Core Foundation object.
    /// </summary>
    /// <param name="cf">A CFType object to release. This value must not be NULL.</param>
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial void CFRelease(IntPtr cf);

    /// <summary>
    /// Creates an immutable string from a C string.
    /// </summary>
    /// <param name="allocator">The allocator to use to allocate memory for the new string. Pass NULL or kCFAllocatorDefault to use the current default allocator.</param>
    /// <param name="cStr">The NULL-terminated C string to be used to create the CFString object. The string must use an 8-bit encoding.</param>
    /// <param name="encoding">The encoding of the characters in the C string. The encoding must specify an 8-bit encoding.</param>
    /// <returns>An immutable string containing cStr (after stripping off the NULL terminating character), or NULL if there was a problem creating the object. Ownership follows the The Create Rule.</returns>
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFStringCreateWithCString(IntPtr allocator, [In] byte[] cStr, CFStringBuiltInEncodings encoding);

    /// <summary>
    /// Copies the character contents of a string to a local C string buffer after converting the characters to a given encoding.
    /// </summary>
    /// <param name="theString">The string whose contents you wish to access.</param>
    /// <param name="buffer">The C string buffer into which to copy the string. On return, the buffer contains the converted characters. If there is an error in conversion, the buffer contains only partial results. The buffer must be large enough to contain the converted characters and a NUL terminator. For example, if the string is Toby, the buffer must be at least 5 bytes long.</param>
    /// <param name="bufferSize">The length of buffer in bytes.</param>
    /// <param name="encoding">The string encoding to which the character contents of theString should be converted. The encoding must specify an 8-bit encoding.</param>
    /// <returns></returns>
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFStringGetCString(IntPtr theString, [Out] byte[] buffer, int bufferSize, CFStringBuiltInEncodings encoding);

    /// <summary>
    /// Returns the value associated with a given key.
    /// </summary>
    /// <param name="theDict">The dictionary to examine.</param>
    /// <param name="key">The key for which to find a match in theDict. The key hash and equal callbacks provided when the dictionary was created are used to compare. If the hash callback was NULL, the key is treated as a pointer and converted to an integer. If the equal callback was NULL, pointer equality (in C, ==) is used. If key, or any of the keys in theDict, is not understood by the equal callback, the behavior is undefined.</param>
    /// <returns>The value associated with key in theDict, or NULL if no key-value pair matching key exists. Since NULL is also a valid value in some dictionaries, use CFDictionaryGetValueIfPresent to distinguish between a value that is not found, and a NULL value. If the value is a Core Foundation object, ownership follows the The Get Rule.</returns>
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    /// <summary>
    /// Returns the string representation of a specified CFUUID object.
    /// </summary>
    /// <param name="allocator">The allocator to use to allocate memory for the new string. Pass NULL or kCFAllocatorDefault to use the current default allocator.</param>
    /// <param name="uuid">The CFUUID object whose string representation to obtain.</param>
    /// <returns>The string representation of uuid. Ownership follows the The Create Rule.</returns>
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFUUIDCreateString(IntPtr allocator, IntPtr uuid);

    /// <summary>
    /// Creates a new CFURL object for a file system entity using the native representation.
    /// </summary>
    /// <param name="allocator">The allocator to use to allocate memory for the new CFURL object. Pass NULL or kCFAllocatorDefault to use the current default allocator.</param>
    /// <param name="buffer">The character bytes to convert into a CFURL object. This should be the path as you would use in POSIX function calls.</param>
    /// <param name="bufLen">The number of character bytes in the buffer (usually the result of a call to strlen), not including any null termination.</param>
    /// <param name="isDirectory">A Boolean value that specifies whether the string is treated as a directory path when resolving against relative path components—true if the pathname indicates a directory, false otherwise.</param>
    /// <returns></returns>
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFURLCreateFromFileSystemRepresentation(IntPtr allocator, byte[] buffer, long bufLen, [MarshalAs(UnmanagedType.U1)] bool isDirectory);

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
        kCFStringEncodingASCII = 0x0600,
        kCFStringEncodingUTF8 = 0x08000100,
    }

    // for debug
    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial int CFDictionaryGetCount(IntPtr theDict);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial void CFDictionaryGetKeysAndValues(IntPtr theDict, IntPtr[] keys, IntPtr[] values);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFGetTypeID(IntPtr cf);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFStringGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFNumberGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFBooleanGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFNumberGetValue(IntPtr number, CFNumberType theType, out int value);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFBooleanGetValue(IntPtr boolean);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    internal static partial IntPtr CFUUIDGetTypeID();

    /// <summary>
    /// Prints the contents of a CFDictionary to the log.
    /// </summary>
    /// <param name="dict">The CFDictionary to print.</param>
    internal static void PrintDict(IntPtr dict)
    {
        var count = CFDictionaryGetCount(dict);
        var keys = new IntPtr[count];
        var values = new IntPtr[count];
        CFDictionaryGetKeysAndValues(dict, keys, values);
        for (var i = 0; i < count; i++)
        {
            var key = keys[i];
            var value = values[i];

            var keyBuffer = new byte[128];
            if (!CFStringGetCString(key, keyBuffer, keyBuffer.Length, CFStringBuiltInEncodings.kCFStringEncodingUTF8))
            {
                continue;
            }

            var keyString = Encoding.UTF8.GetString(keyBuffer);

            var typeId = CFGetTypeID(value);

            if (typeId == CFStringGetTypeID())
            {
                var stringBuffer = new byte[128];
                if (CFStringGetCString(value, stringBuffer, stringBuffer.Length, CFStringBuiltInEncodings.kCFStringEncodingUTF8))
                {
                    Log.Information($"{keyString}: {Encoding.UTF8.GetString(stringBuffer)}");
                }
            }
            else if (typeId == CFNumberGetTypeID())
            {
                if (CFNumberGetValue(value, CFNumberType.kCFNumberIntType, out var intValue))
                {
                    Log.Information($"{keyString}: {intValue}");
                }
            }
            else if (typeId == CFBooleanGetTypeID())
            {
                var boolValue = CFBooleanGetValue(value);
                Log.Information($"{keyString}: {boolValue}");
            }
            else if (typeId == CFUUIDGetTypeID())
            {
                var uuidString = CFUUIDCreateString(IntPtr.Zero, value);
                var uuid = new byte[128];
                if (CFStringGetCString(uuidString, uuid, uuid.Length, CFStringBuiltInEncodings.kCFStringEncodingUTF8))
                {
                    Log.Information($"{keyString}: {Encoding.UTF8.GetString(uuid)}");
                }
            }
            else
            {
                Log.Information($"{keyString}: <unknown type>, type ID: {typeId}");
            }
        }
    }
}
