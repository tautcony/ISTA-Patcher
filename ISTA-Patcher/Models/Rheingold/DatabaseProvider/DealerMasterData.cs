// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Models.Rheingold.DatabaseProvider;

using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = false)]
[DataContract(Name = "DealerMasterData", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class DealerMasterData
{
    public DealerMasterData()
    {
        this.distributionPartner = new DistributionPartner();
    }

    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    [DataMember]
    public DistributionPartner distributionPartner { get; set; }

    [XmlAttribute]
    [DataMember]
    public DateTime expirationDate { get; set; }

    [XmlAttribute]
    [DataMember]
    public string verificationCode { get; set; }

    [XmlAttribute]
    [DataMember]
    public string hardwareId { get; set; }

    public static byte[] Serialize<T>(T data)
    {
        using var ms = new MemoryStream();
        var serializer = new XmlSerializer(typeof(T));
        var ws = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        using var xmlWriter = XmlWriter.Create(ms, ws);
        serializer.Serialize(xmlWriter, data);
        return ms.ToArray();
    }
}
