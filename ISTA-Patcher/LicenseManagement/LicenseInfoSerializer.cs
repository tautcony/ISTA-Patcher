// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.LicenseManagement;

using System.Text;
using System.Xml;
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

            using var memoryStream = new MemoryStream();
            new XmlSerializer(typeof(LicenseInfo)).Serialize(memoryStream, licInfo);
            var result = memoryStream.ToArray();
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while serializing license info");
            return null;
        }
    }

    public static byte[] SerializeLicenseToByteArray(LicenseInfo? licInfo)
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
        var serializedXml = "<?xml version=\"1.0\"?>\n" + Encoding.UTF8.GetString(ms.ToArray());
        serializedXml = serializedXml.ReplaceLineEndings("\r\n");
        var buffer = Encoding.UTF8.GetBytes(serializedXml);
        return buffer;
    }

    public static LicenseInfo? DeserializeFromString(string licString)
    {
        if (string.IsNullOrEmpty(licString))
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(licString));
            return (LicenseInfo)new XmlSerializer(typeof(LicenseInfo)).Deserialize(stream);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DeserializeFromString failed");
        }

        return null;
    }
}
