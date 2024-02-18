// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Utils.LicenseManagement;

using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Serilog;

public static class LicenseInfoSerializer
{
    public static byte[] ToByteArray<T>(T licInfo)
    {
        using var ms = new MemoryStream();
        var serializer = new XmlSerializer(typeof(T));
        var ws = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
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

    public static string ToString<T>(T licInfo)
    {
        return Encoding.UTF8.GetString(ToByteArray(licInfo));
    }

    public static T? FromByteArray<T>(byte[] serializedValue)
        where T : class
    {
        try
        {
            using var stream = new MemoryStream(serializedValue);
            return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Log.Error(ex, "Deserialize from string failed");
        }

        return null;
    }

    public static T? FromString<T>(string serializedValue)
        where T : class
    {
        return FromByteArray<T>(Encoding.UTF8.GetBytes(serializedValue));
    }
}
