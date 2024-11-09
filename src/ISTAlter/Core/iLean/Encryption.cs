// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Core.iLean;

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using ISTAlter.Utils;

public static class Encryption
{
    public static string? MachineGuid { get; set; }

    public static string? VolumeSerialNumber { get; set; }

    [SupportedOSPlatform("Windows")]
    public static void InitializeMachineInfo()
    {
        MachineGuid = NativeMethods.GetMachineGuid();
        VolumeSerialNumber = NativeMethods.GetVolumeSerialNumber();
    }

    public static void InitializeMachineInfo(string machineGuid, string volumeSerialNumber)
    {
        MachineGuid = machineGuid;
        VolumeSerialNumber = volumeSerialNumber;
    }

    internal static Aes InitializeAesProvider()
    {
        if (MachineGuid == null || VolumeSerialNumber == null)
        {
            throw new InvalidOperationException("MachineGuid and VolumeSerialNumber must be initialized.");
        }

        var clientID = ReverseString(MachineGuid);
        var volumeSNr = VolumeSerialNumber;
        var volumeSN = ReverseString(volumeSNr);

        var iv = clientID[..(clientID.Length / 2)];
        var key = volumeSN + clientID[(clientID.Length / 2)..] + volumeSNr;

        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);

        return aes;
    }

    public static string Encrypt(string toEncrypt)
    {
        using var aes = InitializeAesProvider();
        using var memoryStream = new MemoryStream();
        var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        var bytes = Encoding.UTF8.GetBytes(toEncrypt);
        cryptoStream.Write(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public static string Decrypt(string toDecrypt)
    {
        using var aes = InitializeAesProvider();
        using var memoryStream = new MemoryStream();
        var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
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
}
