// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Utils;

using System.Diagnostics;
using System.Runtime.Versioning;

public static partial class NativeMethods
{
    /// <summary>
    /// Retrieves the Machine UUID.
    /// </summary>
    /// <returns>The Machine UUID, or an empty string if not found.</returns>
    [SupportedOSPlatform("Linux")]
    public static string GetLinuxVolumeSerialNumber()
    {
        ProcessStartInfo psi = new()
        {
            FileName = "/bin/bash",
            Arguments = "-c \"findmnt -no UUID /\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return output;
    }

    /// <summary>
    /// Retrieves the Machine ID.
    /// </summary>
    /// <returns>The Machine ID, or an empty string if not found.</returns>
    [SupportedOSPlatform("Linux")]
    public static string GetLinuxMachineId()
    {
        const string EtcMachineId = "/etc/machine-id";

        var ret = string.Empty;
        if (File.Exists(EtcMachineId))
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(EtcMachineId);
                if (lines.Length > 0)
                {
                    ret = lines[0];
                }
            }
            catch
            {
                return ret;
            }
        }

        return ret;
    }
}
