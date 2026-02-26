// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAlter.Models.Rheingold.DatabaseProvider;

using System.Text;
using System.Xml;
using System.Xml.Serialization;

public partial class DealerMasterData
{
    public static byte[] Serialize<T>(T data)
    {
        using var ms = new MemoryStream();
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        using var xmlWriter = XmlWriter.Create(ms, settings);
        serializer.Serialize(xmlWriter, data);
        return ms.ToArray();
    }
}
