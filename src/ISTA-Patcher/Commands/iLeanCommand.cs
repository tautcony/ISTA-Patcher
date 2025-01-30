// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Commands;

using System.Diagnostics.CodeAnalysis;
using DotMake.CommandLine;
using ISTAlter;
using ISTAlter.Core.iLean;
using ISTAlter.Utils;
using Serilog;

[CliCommand(
    Name = "ilean",
    Description = "Perform operations related to iLean.",
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormAutoGenerate = false,
    Parent = typeof(RootCommand)
)]
public class iLeanCommand
{
    public RootCommand? ParentCommand { get; set; }

    [CliOption(Description = "Specify the machine GUID.", Required = false)]
    public string? MachineGuid { get; set; }

    [CliOption(Description = "Specify the volume serial number.", Required = false)]
    public string? VolumeSerialNumber { get; set; }

    [CliOption(Description = "Show the machine information.")]
    public bool ShowMachineInfo { get; set; }

    [CliOption(Description = "Encrypt the provided file/content.", Required = false)]
    public string? Encrypt { get; set; }

    [CliOption(Description = "Decrypt the provided file/content.", Required = false)]
    public string? Decrypt { get; set; }

    public void Run()
    {
        var opts = new ISTAOptions.ILeanOptions
        {
            Verbosity = this.ParentCommand!.Verbosity,
            MachineGuid = this.MachineGuid,
            VolumeSerialNumber = this.VolumeSerialNumber,
            ShowMachineInfo = this.ShowMachineInfo,
            Encrypt = this.Encrypt,
            Decrypt = this.Decrypt,
        };

        Execute(opts).Wait();
    }

    [SuppressMessage("Interoperability", "CA1416", Justification = "SupportedOSPlatform attribute is used")]
    public static async Task<int> Execute(ISTAOptions.ILeanOptions opts)
    {
        using var transaction = new TransactionHandler("ISTA-Patcher", "ilean");
        opts.Transaction = transaction;
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

        try
        {
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
        }
        catch (Exception ex)
        {
            Log.Information("MachineGuid: {MachineGuid}, VolumeSerialNumber: {VolumeSerialNumber}", opts.MachineGuid, opts.VolumeSerialNumber);
            Log.Error(ex, "iLean Encryption/Decryption failed.");
            return -1;
        }

        Log.Warning("No operation matched, exiting...");
        return -1;
    }
}
