namespace ISTA_Patcher.LicenseManagement;

using System.Security.Cryptography;
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
        var memoryStream = new MemoryStream();
        new XmlSerializer(typeof(LicenseInfo)).Serialize(memoryStream, licInfo);
        var buffer = memoryStream.GetBuffer();

        // Log.Debug("licInfo stream: {ByteArray}", FormatConverter.ByteArray2String(buffer, (uint)buffer.Length));
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
