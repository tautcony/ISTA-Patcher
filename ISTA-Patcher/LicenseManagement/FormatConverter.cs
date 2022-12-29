namespace ISTA_Patcher.LicenseManagement;

using System.Globalization;
using System.Text;
using Serilog;

public static class FormatConverter
{
    public static string ByteArray2String(byte[] param, uint paramlen)
    {
        try
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < paramlen; i++)
            {
                stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:X2}", param[i]));
            }

            return stringBuilder.ToString();
        }
        catch (Exception exception)
        {
            Log.Error("Error in ByteArray2String: {Message}", exception.Message);
        }

        return string.Empty;
    }
}
