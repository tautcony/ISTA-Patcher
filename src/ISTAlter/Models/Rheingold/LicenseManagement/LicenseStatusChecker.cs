// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Models.Rheingold.LicenseManagement;

using System.Security.Cryptography;
using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;
using Serilog;

public static class LicenseStatusChecker
{
    public static bool IsLicenseValid(LicenseInfo testLicInfo, RSAPKCS1SignatureDeformatter signatureDeformatter)
    {
        var licenseInfo = (LicenseInfo)testLicInfo.Clone();
        var licenseKey = licenseInfo.LicenseKey;
        if (licenseKey == null)
        {
            return false;
        }

        licenseInfo.LicenseKey = [];
        var hashValue = GetHashValueFrom(licenseInfo);

        Log.Debug("hash stream: {ByteArray}", Convert.ToHexString(hashValue));

        if (signatureDeformatter.VerifySignature(hashValue, licenseKey))
        {
            return true;
        }

        Log.Warning("Signature verification failed");
        return false;
    }

    public static void GenerateLicenseKey(LicenseInfo licenseInfo, string xmlString)
    {
        using var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
        rsaCryptoServiceProvider.FromXmlString(xmlString);
        licenseInfo.LicenseKey = [];
        var hashValue = GetHashValueFrom(licenseInfo);
        var signatureFormatter = new RSAPKCS1SignatureFormatter(rsaCryptoServiceProvider);
        signatureFormatter.SetHashAlgorithm("SHA1");

        licenseInfo.LicenseKey = signatureFormatter.CreateSignature(hashValue);
    }

    private static byte[] GetHashValueFrom(LicenseInfo licInfo)
    {
        var serializedXmlByte = LicenseInfoSerializer.ToByteArray(licInfo);
        var bufferLength = Math.Max(256, (uint)Math.Pow(2, Math.Ceiling(Math.Log2(serializedXmlByte.Length))));
        var buffer = new byte[bufferLength];
        Array.Copy(serializedXmlByte, buffer, serializedXmlByte.Length);

        Log.Debug("licInfo stream: {ByteArray}", Convert.ToHexString(buffer));
        return SHA1.HashData(buffer);
    }

    public static RSAPKCS1SignatureDeformatter GetRSAPKCS1SignatureDeformatter(string xmlString)
    {
        var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
        rsaCryptoServiceProvider.FromXmlString(xmlString);
        var signatureDeformatter = new RSAPKCS1SignatureDeformatter(rsaCryptoServiceProvider);
        signatureDeformatter.SetHashAlgorithm("SHA1");

        return signatureDeformatter;
    }
}
