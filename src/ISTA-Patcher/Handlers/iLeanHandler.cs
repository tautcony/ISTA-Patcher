// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using System.Diagnostics.CodeAnalysis;
using ISTAlter;
using ISTAlter.Core.iLean;
using ISTAlter.Utils;
using Serilog;

public static class iLeanHandler
{
    [SuppressMessage("Interoperability", "CA1416", Justification = "SupportedOSPlatform attribute is used")]
    public static async Task<int> Execute(ISTAOptions.ILeanOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;
        var isSupportedPlatform = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

        if (opts.ShowMachineInfo)
        {
            if (isSupportedPlatform)
            {
                Log.Information("MachineGuid: {MachineGuid}", NativeMethods.GetMachineUUID());
                Log.Information("VolumeSerialNumber: {VolumeSerialNumber}", NativeMethods.GetVolumeSerialNumber());
            }
            else
            {
                Log.Error("--show-machine-info option is not supported on this platform");
            }

            return 0;
        }

        if (string.IsNullOrEmpty(opts.Encrypt) && string.IsNullOrEmpty(opts.Decrypt))
        {
            Log.Warning("No operation matched, exiting...");
            return 1;
        }

        if (string.IsNullOrEmpty(opts.MachineGuid) && isSupportedPlatform)
        {
            Log.Information("MachineGuid is not provided, read from system...");
            opts.MachineGuid = NativeMethods.GetMachineUUID();
        }

        if (string.IsNullOrEmpty(opts.VolumeSerialNumber) && isSupportedPlatform)
        {
            Log.Information("VolumeSerialNumber is not provided, read from system...");
            opts.VolumeSerialNumber = NativeMethods.GetVolumeSerialNumber();
        }

        if (string.IsNullOrEmpty(opts.MachineGuid) || string.IsNullOrEmpty(opts.VolumeSerialNumber))
        {
            Log.Error("MachineGuid or VolumeSerialNumber is not available, exiting...");
            return -1;
        }

        using var encryption = new iLeanCipher(opts.MachineGuid, opts.VolumeSerialNumber);
        if (!string.IsNullOrEmpty(opts.Encrypt))
        {
            var content = File.Exists(opts.Encrypt)
                ? await File.ReadAllTextAsync(opts.Encrypt).ConfigureAwait(false)
                : opts.Encrypt;
            var encrypted = encryption.Encrypt(content);
            Log.Information("Encrypted: \n{Encrypted}\n", encrypted);
            return 0;
        }

        if (!string.IsNullOrEmpty(opts.Decrypt))
        {
            var content = File.Exists(opts.Decrypt)
                ? await File.ReadAllTextAsync(opts.Decrypt).ConfigureAwait(false)
                : opts.Decrypt;
            var decrypted = encryption.Decrypt(content);
            Log.Information("Decrypted: \n{Decrypted}\n", decrypted);
            return 0;
        }

        Log.Warning("No operation matched, exiting...");
        return 1;
    }
}
