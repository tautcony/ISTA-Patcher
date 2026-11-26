// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAPatcher.Commands;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
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
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Parent = typeof(RootCommand)
)]
public class iLeanCommand
{
    public RootCommand? ParentCommand { get; set; }

    public CancellationToken CancellationToken { get; set; }

    [CliOption(Description = "Specify the cipher type.", Required = false)]
    public ISTAOptions.CipherType CipherType { get; set; } = ISTAOptions.CipherType.DefaultCipher;

    [CliOption(Description = "Specify the machine GUID.", Required = false)]
    public string? MachineGuid { get; set; }

    [CliOption(Description = "Specify the volume serial number.", Required = false)]
    public string? VolumeSerialNumber { get; set; }

    [CliOption(Description = "Specify the password.", Required = false)]
    public string? Password { get; set; }

    [CliOption(Description = "Show the machine information.")]
    public bool ShowMachineInfo { get; set; }

    [CliOption(Description = "Encrypt the provided file/content.", Required = false)]
    public string? Encrypt { get; set; }

    [CliOption(Description = "Decrypt the provided file/content.", Required = false)]
    public string? Decrypt { get; set; }

    [CliOption(Description = "Output the result to a file.", Required = false)]
    public string? Output { get; set; }

    [CliOption(Description = "Specify the formatter type.", Required = false)]
    public ISTAOptions.FormatterType Formatter { get; set; } = ISTAOptions.FormatterType.Default;

    private static readonly JsonSerializerOptions DefaultJsonSerializerOption = new() { WriteIndented = true };

    public async Task<int> RunAsync()
    {
        var opts = new ISTAOptions.ILeanOptions
        {
            Verbosity = this.ParentCommand!.Verbosity,
            CipherType = this.CipherType,
            MachineGuid = this.MachineGuid,
            VolumeSerialNumber = this.VolumeSerialNumber,
            Password = this.Password,
            ShowMachineInfo = this.ShowMachineInfo,
            Encrypt = this.Encrypt,
            Decrypt = this.Decrypt,
            Output = this.Output,
            CancellationToken = this.CancellationToken.CanBeCanceled ? this.CancellationToken : Global.CancellationToken,
        };

        return await Execute(opts);
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
                Log.Warning("--show-machine-info is not supported on this platform.");
            }

            return 0;
        }

        if (string.IsNullOrEmpty(opts.Encrypt) && string.IsNullOrEmpty(opts.Decrypt))
        {
            Log.Warning("No operation provided, please provide Encrypt or Decrypt option.");
            return -1;
        }

        if (!string.IsNullOrEmpty(opts.Encrypt) && !string.IsNullOrEmpty(opts.Decrypt))
        {
            Log.Warning("Both Encrypt and Decrypt options are provided, please provide only one.");
            return -1;
        }

        string result = null;

        if (opts.CipherType == ISTAOptions.CipherType.DefaultCipher)
        {
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
                Log.Warning("MachineGuid and VolumeSerialNumber are required for this cipher type.");
                return -1;
            }

            result = await iLeanCipherHandler(opts);
        }

        if (opts.CipherType == ISTAOptions.CipherType.PasswordCipher)
        {
            if (string.IsNullOrEmpty(opts.Password))
            {
                Log.Warning("A password is required for this cipher type.");
                return -1;
            }

            result = await iLeanCipherPasswordHandler(opts);
        }

        if (result == null)
        {
            Log.Warning("The operation could not be completed. Check the selected operation and input values.");
            return -1;
        }

        if (opts.Decrypt is { Length: > 0 })
        {
            switch (opts.Formatter)
            {
                case ISTAOptions.FormatterType.JSON:
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);
                    result = JsonSerializer.Serialize(jsonElement, DefaultJsonSerializerOption);
                    break;
                }

                case ISTAOptions.FormatterType.XML:
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(result);
                    await using var stringWriter = new StringWriter();
                    await using var xmlTextWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true });
                    xmlDoc.WriteTo(xmlTextWriter);
                    await xmlTextWriter.FlushAsync();
                    result = stringWriter.GetStringBuilder().ToString();

                    break;
                }

                case ISTAOptions.FormatterType.Default:
                default:
                    break;
            }
        }

        if (!string.IsNullOrEmpty(opts.Output))
        {
            try
            {
                await File.WriteAllTextAsync(opts.Output, result, opts.CancellationToken).ConfigureAwait(false);
                Log.Information("Result is written to {Output}", opts.Output);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warning(ex, "Could not write output file; check write permissions.");
                return -1;
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Could not write output file; close applications using the file and retry.");
                return -1;
            }
        }
        else
        {
            Log.Information("Result:\n {Result}", result);
        }

        return 0;
    }

    private static async Task<string?> iLeanCipherHandler(ISTAOptions.ILeanOptions opts)
    {
        try
        {
            using var encryption = new iLeanCipher(opts.MachineGuid!, opts.VolumeSerialNumber!);
            opts.CancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(opts.Encrypt))
            {
                var content = File.Exists(opts.Encrypt)
                    ? await File.ReadAllTextAsync(opts.Encrypt, opts.CancellationToken).ConfigureAwait(false)
                    : opts.Encrypt;
                return encryption.Encrypt(content);
            }

            if (!string.IsNullOrEmpty(opts.Decrypt))
            {
                var content = File.Exists(opts.Decrypt)
                    ? await File.ReadAllTextAsync(opts.Decrypt, opts.CancellationToken).ConfigureAwait(false)
                    : opts.Decrypt;
                return encryption.Decrypt(content);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            Log.Warning("iLean input could not be decrypted. Check the encrypted content and credentials.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning(ex, "Could not read iLean input file; check read permissions.");
        }
        catch (IOException ex)
        {
            Log.Warning(ex, "Could not read iLean input file; close applications using the file and retry.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "iLean Encryption/Decryption failed (credentials omitted from logs).");
        }

        return null;
    }

    private static async Task<string?> iLeanCipherPasswordHandler(ISTAOptions.ILeanOptions opts)
    {
        try
        {
            using var encryption = new iLeanPasswordCipher(opts.Password!);
            opts.CancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(opts.Encrypt))
            {
                var content = File.Exists(opts.Encrypt)
                    ? await File.ReadAllTextAsync(opts.Encrypt, opts.CancellationToken).ConfigureAwait(false)
                    : opts.Encrypt;
                return encryption.Encrypt(content);
            }

            if (!string.IsNullOrEmpty(opts.Decrypt))
            {
                var content = File.Exists(opts.Decrypt)
                    ? await File.ReadAllTextAsync(opts.Decrypt, opts.CancellationToken).ConfigureAwait(false)
                    : opts.Decrypt;
                return encryption.Decrypt(content);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            Log.Warning("iLean input could not be decrypted. Check the encrypted content and password.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning(ex, "Could not read iLean input file; check read permissions.");
        }
        catch (IOException ex)
        {
            Log.Warning(ex, "Could not read iLean input file; close applications using the file and retry.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "iLean Password Encryption/Decryption failed (password omitted from logs).");
        }

        return null;
    }
}
