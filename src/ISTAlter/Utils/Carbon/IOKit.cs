// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Utils.Carbon;

using System.Runtime.InteropServices;
using CFDictionaryRef = System.IntPtr;
using CFStringRef = System.IntPtr;

internal static partial class IOKit
{
    private const string IOKitLibrary = "/System/Library/Frameworks/IOKit.framework/IOKit";

    /// <summary>
    /// Create a matching dictionary that specifies an IOService class match.
    /// </summary>
    /// <param name="name">The class name, as a const C-string. Class matching is successful on IOService's of this class or any subclass.</param>
    /// <returns>The matching dictionary created, is returned on success, or zero on failure. The dictionary is commonly passed to IOServiceGetMatchingServices or IOServiceAddNotification which will consume a reference, otherwise it should be released with CFRelease by the caller.</returns>
    [LibraryImport(IOKitLibrary, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr IOServiceMatching(string name);

    /// <summary>
    /// Look up a registered IOService object that matches a matching dictionary.
    /// </summary>
    /// <param name="masterPort">The primary port obtained from IOMasterPort. Pass kIOMasterPortDefault to look up the default primary port.</param>
    /// <param name="matchingDictionary">A CF dictionary containing matching information, of which one reference is always consumed by this function (Note prior to the Tiger release there was a small chance that the dictionary might not be released if there was an error attempting to serialize the dictionary). IOKitLib can construct matching dictionaries for common criteria with helper functions such as IOServiceMatching, IOServiceNameMatching, IOBSDNameMatching.</param>
    /// <returns>The first service matched is returned on success. The service must be released by the caller.</returns>
    [LibraryImport(IOKitLibrary)]
    internal static partial IntPtr IOServiceGetMatchingService(IntPtr masterPort, CFDictionaryRef matchingDictionary);

    /// <summary>
    /// Create a CF representation of a registry entry's property.
    /// </summary>
    /// <param name="entry">The registry entry handle whose property to copy.</param>
    /// <param name="key">A CFString specifying the property name.</param>
    /// <param name="allocator">The CF allocator to use when creating the CF container.</param>
    /// <param name="options">No options are currently defined.</param>
    /// <returns>This function creates an instantaneous snapshot of a registry entry property, creating a CF container analogue in the caller's task. Not every object available in the kernel is represented as a CF container; currently OSDictionary, OSArray, OSSet, OSSymbol, OSString, OSData, OSNumber, OSBoolean are created as their CF counterparts.</returns>
    [LibraryImport(IOKitLibrary)]
    internal static partial SafeCreateHandle IORegistryEntryCreateCFProperty(IntPtr entry, CFStringRef key, IntPtr allocator, uint options);

    /// <summary>
    /// Releases an object handle previously returned by IOKitLib.
    /// </summary>
    /// <param name="object">The IOKit object to release.</param>
    /// <returns>A kern_return_t error code.</returns>
    [LibraryImport(IOKitLibrary)]
    internal static partial int IOObjectRelease(IntPtr @object);
}
