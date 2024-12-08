// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using ISTAlter;
using ISTAlter.Core.iLean;
using ISTAlter.Utils;
using Serilog;

public static class iLeanHandler
{
    public static async Task<int> Execute(ISTAOptions.ILeanOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;

        if (opts.ShowMachineInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
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

        if (!string.IsNullOrEmpty(opts.MachineGuid) && !string.IsNullOrEmpty(opts.VolumeSerialNumber))
        {
            Encryption.InitializeMachineInfo(opts.MachineGuid, opts.VolumeSerialNumber);
        }
        else
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                Encryption.InitializeMachineInfo();
            }
            else
            {
                Log.Error("MachineGuid and VolumeSerialNumber must be initialized on this platform");
                return -1;
            }
        }

        if (opts.Encrypt != null)
        {
            var content = File.Exists(opts.Encrypt)
                ? await File.ReadAllTextAsync(opts.Encrypt).ConfigureAwait(false)
                : opts.Encrypt;
            var encrypted = Encryption.Encrypt(content);
            Log.Information("Encrypted: \n{Encrypted}\n", encrypted);
            return 0;
        }

        if (opts.Decrypt != null)
        {
            var content = File.Exists(opts.Decrypt)
                ? await File.ReadAllTextAsync(opts.Decrypt).ConfigureAwait(false)
                : opts.Decrypt;
            var decrypted = Encryption.Decrypt(content);
            Log.Information("Decrypted: \n{Decrypted}\n", decrypted);
            return 0;
        }

        Log.Warning("No operation matched, exiting...");
        return 1;
    }
}
