namespace ISTA_Patcher.LicenseManagement;

using System.Security.Cryptography;
using System.Xml.Serialization;
using ISTA_Patcher.LicenseManagement.CoreFramework;
using Serilog;

public class LicenseStatusChecker
{
    public static bool IsLicenseValid(LicenseInfo testLicInfo, string modulus, string exponent)
    {
        var licenseInfo = (LicenseInfo)testLicInfo.Clone();
        var licenseKey = licenseInfo.LicenseKey;
        if (licenseKey == null)
        {
            return false;
        }

        licenseInfo.LicenseKey = Array.Empty<byte>();
        var hashValue = GetHashValueFrom(licenseInfo);

        if (GetRSAPKCS1SignatureDeformatter(modulus, exponent).VerifySignature(hashValue, licenseKey))
        {
            return true;
        }

        Log.Warning("Signature verification failed");
        return false;
    }

    private static byte[] GetHashValueFrom(LicenseInfo licInfo)
    {
        var memoryStream = new MemoryStream();
        new XmlSerializer(typeof(LicenseInfo)).Serialize(memoryStream, licInfo);
        var buffer = memoryStream.GetBuffer();
        Log.Information("licInfo stream: {ByteArray}", FormatConverter.ByteArray2String(buffer, (uint)buffer.Length));
        return SHA1.Create().ComputeHash(buffer);
    }

    private static RSAPKCS1SignatureDeformatter GetRSAPKCS1SignatureDeformatter(string modulus, string exponent)
    {
        // the function which need to be patched in ISTA
        var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
        rsaCryptoServiceProvider.ImportParameters(new RSAParameters
        {
            Modulus = Convert.FromBase64String(modulus),
            Exponent = Convert.FromBase64String(exponent),
        });
        var rsaPKCS1SignatureDeformatter = new RSAPKCS1SignatureDeformatter(rsaCryptoServiceProvider);
        rsaPKCS1SignatureDeformatter.SetHashAlgorithm("SHA1");

        return rsaPKCS1SignatureDeformatter;
    }

    private static RSAPKCS1SignatureFormatter GetRSAPKCS1SignatureFormatter(string modulus, string exponent)
    {
        var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
        rsaCryptoServiceProvider.ImportParameters(new RSAParameters
        {
            Modulus = Convert.FromBase64String(modulus),
            Exponent = Convert.FromBase64String(exponent),
        });
        var rsaPKCS1SignatureFormatter = new RSAPKCS1SignatureFormatter(rsaCryptoServiceProvider);
        rsaPKCS1SignatureFormatter.SetHashAlgorithm("SHA1");

        return rsaPKCS1SignatureFormatter;
    }
}
