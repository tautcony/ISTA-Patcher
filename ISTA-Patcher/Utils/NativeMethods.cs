// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTA_Patcher.Utils;

using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

internal static partial class NativeMethods
{
    /// <summary>
    /// Gets a value that indicates whether the assembly manifest at the supplied path contains a strong name signature.
    /// </summary>
    /// <param name="wszFilePath">[in] The path to the portable executable (.exe or .dll) file for the assembly to be verified.</param>
    /// <param name="fForceVerification">[in] true to perform verification, even if it is necessary to override registry settings; otherwise, false.</param>
    /// <param name="pfWasVerified">[out] true if the strong name signature was verified; otherwise, false. pfWasVerified is also set to false if the verification was successful due to registry settings.</param>
    /// <returns>S_OK if the verification was successful; otherwise, an HRESULT value that indicates failure.</returns>
    [SupportedOSPlatform("Windows")]
    [LibraryImport("mscoree.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "StrongNameSignatureVerificationEx")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool StrongNameSignatureVerificationEx(
        [MarshalAs(UnmanagedType.LPWStr)]string wszFilePath,
        [MarshalAs(UnmanagedType.U1)]bool fForceVerification,
        [MarshalAs(UnmanagedType.U1)]ref bool pfWasVerified
    );

    /// <summary>
    /// Retrieves the volume serial number of the drive where the system directory is located.
    /// The returned serial number is truncated to the first 8 characters.
    /// </summary>
    /// <returns>The truncated volume serial number of the system drive.</returns>
    [SupportedOSPlatform("Windows")]
    public static string GetVolumeSerialNumber()
    {
        var volumeSerialNumber = string.Empty;
        var driveLetter = Path.GetPathRoot(Environment.SystemDirectory);
        driveLetter = driveLetter?.Replace("\\", string.Empty, StringComparison.Ordinal);

        var query = $"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'";

        using var searcher = new ManagementObjectSearcher(query);
        volumeSerialNumber = searcher.Get()
                                         .Cast<ManagementObject>()
                                         .FirstOrDefault(disk => disk["VolumeSerialNumber"] != null)?
                                         .GetPropertyValue("VolumeSerialNumber")
                                         .ToString();
        if (string.IsNullOrEmpty(volumeSerialNumber))
        {
            volumeSerialNumber = "a";
        }

        volumeSerialNumber = volumeSerialNumber.Replace("-", string.Empty, StringComparison.Ordinal);
        volumeSerialNumber += "12345678";
        return volumeSerialNumber[..8];
    }

    /// <summary>
    /// Retrieves the Machine GUID from the Windows registry.
    /// The returned GUID is in uppercase and without hyphens.
    /// </summary>
    /// <returns>The Machine GUID in uppercase without hyphens, or an empty string if not found.</returns>
    [SupportedOSPlatform("Windows")]
    public static string GetMachineGuid()
    {
        using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var registryKey = localMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography\");
        var guid = registryKey?.GetValue("MachineGuid")?.ToString();
        if (string.IsNullOrEmpty(guid) || guid.Length < 16)
        {
            return guid ?? string.Empty;
        }

        return guid.ToUpper(CultureInfo.InvariantCulture).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
