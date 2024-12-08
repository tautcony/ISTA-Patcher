// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Utils;

using System.Globalization;
using System.Runtime.Versioning;

public static partial class NativeMethods
{
    /// <summary>
    /// Retrieves the Machine UUID.
    /// The returned UUID is in uppercase and without hyphens.
    /// </summary>
    /// <returns>The Machine UUID in uppercase without hyphens, or an empty string if not found.</returns>
    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("macOS")]
    [SupportedOSPlatform("Linux")]
    public static string GetMachineUUID()
    {
        var uuid = string.Empty;

        if (OperatingSystem.IsWindows())
        {
            uuid = GetWindowsMachineGuid();
        }

        if (OperatingSystem.IsMacOS())
        {
            uuid = GetIOPlatformUUID();
        }

        if (OperatingSystem.IsLinux())
        {
            uuid = GetLinuxMachineId();
        }

        if (string.IsNullOrEmpty(uuid) || uuid.Length < 16)
        {
            return uuid ?? string.Empty;
        }

        return uuid.ToUpper(CultureInfo.InvariantCulture).Replace("-", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Retrieves the volume serial number of the drive where the system directory is located.
    /// The returned serial number is truncated to the first 8 characters.
    /// </summary>
    /// <returns>The truncated volume serial number of the system drive.</returns>
    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("macOS")]
    [SupportedOSPlatform("Linux")]
    public static string GetVolumeSerialNumber()
    {
        var volumeSerialNumber = string.Empty;

        if (OperatingSystem.IsWindows())
        {
            volumeSerialNumber = GetWindowsVolumeSerialNumber();
        }

        if (OperatingSystem.IsMacOS())
        {
            volumeSerialNumber = GetMacVolumeSerialNumber();
        }

        if (OperatingSystem.IsLinux())
        {
            volumeSerialNumber = GetLinuxVolumeSerialNumber();
        }

        if (string.IsNullOrEmpty(volumeSerialNumber))
        {
            volumeSerialNumber = "a";
        }

        volumeSerialNumber = volumeSerialNumber.Replace("-", string.Empty, StringComparison.Ordinal);
        volumeSerialNumber += "12345678";
        return volumeSerialNumber[..8];
    }
}
