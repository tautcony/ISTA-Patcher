// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Core.iLean;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Serilog;

public sealed class iLeanPasswordCipher : IDisposable
{
    private readonly string password;

    private readonly Aes aesInstance;

    public iLeanPasswordCipher(string password)
    {
        this.password = password;
        this.aesInstance = InitializeAesProvider(this.password);
    }

    internal static Aes InitializeAesProvider(string password)
    {
        var aes = Aes.Create();
        var key = GetMd5Hash(password);
        var hash = GetMd5Hash(ReverseString(password));
        var iv = hash[..(hash.Length / 2)];
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

        try
        {
            using MemoryStream memoryStream = new();
            var bytes = Encoding.UTF8.GetBytes(toEncrypt);
            using CryptoStream cryptoStream = new(memoryStream, this.aesInstance.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            return Convert.ToBase64String(memoryStream.ToArray());
        }
        catch (CryptographicException ex)
        {
            Log.Information("Password: {Password}", this.password);
            Log.Error(ex, "iLean Password Encryption failed.");
        }

        return string.Empty;
    }

    public string Decrypt(string toDecrypt)
    {
        if (string.IsNullOrEmpty(toDecrypt))
        {
            return string.Empty;
        }

        try
        {
            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new(memoryStream, this.aesInstance.CreateDecryptor(), CryptoStreamMode.Write);
            var array = Convert.FromBase64String(toDecrypt);
            cryptoStream.Write(array, 0, array.Length);
            cryptoStream.FlushFinalBlock();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch (CryptographicException ex)
        {
            Log.Information("Password: {Password}", this.password);
            Log.Error(ex, "iLean Decryption failed.");
        }

        return string.Empty;
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

    private static string GetMd5Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var array = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(32);
        foreach (var b in array)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        this.aesInstance.Dispose();
    }
}
