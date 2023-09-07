// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.Utils.LicenseManagement;

using System.Globalization;
using System.Text;
using Serilog;

public static class FormatConverter
{
    /// <summary>
    /// Converts a byte array to a hexadecimal string representation.
    /// </summary>
    /// <param name="param">The byte array to convert.</param>
    /// <param name="paramLen">The length of the byte array.</param>
    /// <returns>The hexadecimal string representation of the byte array.</returns>
    public static string ByteArray2String(byte[] param, uint paramLen)
    {
        try
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < paramLen; i++)
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
