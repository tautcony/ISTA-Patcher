// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Core.iLean;

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using ISTAlter.Utils;

public sealed class iLeanCipher : IDisposable
{
    private readonly Aes aesInstance;

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("macOS")]
    [SupportedOSPlatform("Linux")]
    public iLeanCipher()
    {
        var machineGuid = NativeMethods.GetMachineUUID();
        var volumeSerialNumber = NativeMethods.GetVolumeSerialNumber();
        this.aesInstance = InitializeAesProvider(machineGuid, volumeSerialNumber);
    }

    public iLeanCipher(string machineGuid, string volumeSerialNumber)
    {
        if (machineGuid.Length != 32 || volumeSerialNumber.Length != 8)
        {
            throw new InvalidOperationException("MachineGuid and VolumeSerialNumber must be 32 and 8 characters long.");
        }

        this.aesInstance = InitializeAesProvider(machineGuid, volumeSerialNumber);
    }

    internal static Aes InitializeAesProvider(string machineGuid, string volumeSerialNumber)
    {
        if (machineGuid == null || volumeSerialNumber == null)
        {
            throw new InvalidOperationException("MachineGuid and VolumeSerialNumber must be initialized.");
        }

        var clientID = ReverseString(machineGuid);
        var volumeSNr = volumeSerialNumber;
        var volumeSN = ReverseString(volumeSNr);

        var iv = clientID[..(clientID.Length / 2)];
        var key = volumeSN + clientID[(clientID.Length / 2)..] + volumeSNr;

        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);

        return aes;
    }

    public string Encrypt(string toEncrypt)
    {
        if (string.IsNullOrEmpty(toEncrypt))
        {
            return string.Empty;
        }

        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, this.aesInstance.CreateEncryptor(), CryptoStreamMode.Write);
        var bytes = Encoding.UTF8.GetBytes(toEncrypt);
        cryptoStream.Write(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public string Decrypt(string toDecrypt)
    {
        if (string.IsNullOrEmpty(toDecrypt))
        {
            return string.Empty;
        }

        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, this.aesInstance.CreateDecryptor(), CryptoStreamMode.Write);
        var bytes = Convert.FromBase64String(toDecrypt);
        cryptoStream.Write(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static string ReverseString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var array = value.ToCharArray();
        Array.Reverse(array);
        return new string(array);
    }

    public void Dispose()
    {
        this.aesInstance.Dispose();
    }
}
