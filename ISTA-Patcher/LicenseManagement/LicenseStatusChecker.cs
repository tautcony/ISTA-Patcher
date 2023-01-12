// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022 TautCony
namespace ISTA_Patcher.LicenseManagement;

using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ISTA_Patcher.LicenseManagement.CoreFramework;
using Serilog;

public class LicenseStatusChecker
{
    public static bool IsLicenseValid(LicenseInfo testLicInfo, RSAPKCS1SignatureDeformatter signatureDeformatter)
    {
        var licenseInfo = (LicenseInfo)testLicInfo.Clone();
        var licenseKey = licenseInfo.LicenseKey;
        if (licenseKey == null)
        {
            return false;
        }

        licenseInfo.LicenseKey = Array.Empty<byte>();
        var hashValue = GetHashValueFrom(licenseInfo);

        Log.Debug("hash stream: {ByteArray}", FormatConverter.ByteArray2String(hashValue, (uint)hashValue.Length));

        if (signatureDeformatter.VerifySignature(hashValue, licenseKey))
        {
            return true;
        }

        Log.Warning("Signature verification failed");
        return false;
    }

    public static void GenerateLicenseKey(LicenseInfo licenseInfo, string xmlString)
    {
        var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
        rsaCryptoServiceProvider.FromXmlString(xmlString);
        licenseInfo.LicenseKey = Array.Empty<byte>();
        var hashValue = GetHashValueFrom(licenseInfo);
        var signatureFormatter = new RSAPKCS1SignatureFormatter(rsaCryptoServiceProvider);
        signatureFormatter.SetHashAlgorithm("SHA1");

        licenseInfo.LicenseKey = signatureFormatter.CreateSignature(hashValue);
    }

    private static byte[] GetHashValueFrom(LicenseInfo licInfo)
    {
        using var ms = new MemoryStream();
        var serializer = new XmlSerializer(typeof(LicenseInfo));
        var ws = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true,
        };

        using var xmlWriter = XmlWriter.Create(ms, ws);
        serializer.Serialize(xmlWriter, licInfo);
        var serializedXml = "<?xml version=\"1.0\"?>\n" + Encoding.UTF8.GetString(ms.GetBuffer());
        serializedXml = serializedXml.ReplaceLineEndings("\r\n");
        var serializedXmlByte = Encoding.UTF8.GetBytes(serializedXml);
        var bufferLength = (uint)Math.Pow(2, Math.Ceiling(Math.Log2(serializedXmlByte.Length)));
        var buffer = new byte[bufferLength];
        Array.Copy(serializedXmlByte, buffer, serializedXmlByte.Length);

        Log.Debug("licInfo stream: {ByteArray}", FormatConverter.ByteArray2String(buffer, (uint)buffer.Length));
        return SHA1.Create().ComputeHash(buffer);
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
