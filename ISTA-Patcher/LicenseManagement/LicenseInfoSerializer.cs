namespace ISTA_Patcher.LicenseManagement;

using System.Text;
using System.Xml.Serialization;
using ISTA_Patcher.LicenseManagement.CoreFramework;
using Serilog;

public static class LicenseInfoSerializer
{
    public static byte[]? SerializeRequestToByteArray(LicenseInfo? licInfo)
    {
        try
        {
            if (licInfo == null)
            {
                Log.Warning("license info empty");
                return null;
            }

            var memoryStream = new MemoryStream();
            new XmlSerializer(typeof(LicenseInfo)).Serialize(memoryStream, licInfo);
            var result = memoryStream.ToArray();
            memoryStream.Close();
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while serializing license info");
            return null;
        }
    }

    public static bool SerializeRequest(string fileName, LicenseInfo? licInfo)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Log.Warning("fileName empty");
                return false;
            }

            if (licInfo == null)
            {
                Log.Warning("license info empty");
                return false;
            }

            using var fileStream = File.Create(fileName);
            new XmlSerializer(typeof(LicenseInfo)).Serialize(fileStream, licInfo);
            fileStream.Close();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SerializeRequest failed");
        }

        return false;
    }

    public static LicenseInfo? DeserializeFromString(string licString)
    {
        if (string.IsNullOrEmpty(licString))
        {
            return null;
        }

        try
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(licString));
            return (LicenseInfo)new XmlSerializer(typeof(LicenseInfo)).Deserialize(stream);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DeserializeFromString failed");
        }

        return null;
    }
}
