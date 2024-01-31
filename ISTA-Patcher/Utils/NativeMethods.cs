// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTA_Patcher.Utils;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
}
