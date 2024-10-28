// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using ISTA_Patcher.Core.iLean;
using ISTA_Patcher.Utils;
using Serilog;

public class iLeanHandler
{
    public static async Task<int> Execute(ProgramArgs.ILeanOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;

        if (opts.ShowMachineInfo)
        {
            if (OperatingSystem.IsWindows())
            {
                Log.Information("MachineGuid: {MachineGuid}", NativeMethods.GetMachineGuid());
                Log.Information("VolumeSerialNumber: {VolumeSerialNumber}", NativeMethods.GetVolumeSerialNumber());
            }
            else
            {
                Log.Error("--show-machine-info option is not supported on this platform");
                return -1;
            }

            return 0;
        }

        if (!string.IsNullOrEmpty(opts.MachineGuid) && !string.IsNullOrEmpty(opts.VolumeSerialNumber))
        {
            Encryption.InitializeMachineInfo(opts.MachineGuid, opts.VolumeSerialNumber);
        }
        else
        {
            if (OperatingSystem.IsWindows())
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
            var encrypted = Encryption.Encrypt(await File.ReadAllTextAsync(opts.Encrypt).ConfigureAwait(false));
            Log.Information("Encrypted: \n{Encrypted}\n", encrypted);
            return 0;
        }

        if (opts.Decrypt != null)
        {
            var decrypted = Encryption.Decrypt(await File.ReadAllTextAsync(opts.Decrypt).ConfigureAwait(false));
            Log.Information("Decrypted: \n{Decrypted}\n", decrypted);
            return 0;
        }

        Log.Warning("No operation matched, exiting...");
        return 1;
    }
}
