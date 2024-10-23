// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Models.Rheingold.DatabaseProvider;

using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = true)]
[DataContract(Name = "Communication", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class Communication
{
    public Communication()
    {
        this.fax = new Phone();
        this.voice = new Phone();
    }

    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    [DataMember]
    public Phone voice { get; set; }

    [XmlElement(Form = XmlSchemaForm.Unqualified, IsNullable = true)]
    [DataMember]
    public Phone fax { get; set; }

    [XmlAttribute]
    [DataMember]
    public string email { get; set; }

    [XmlAttribute(DataType = "anyURI")]
    [DataMember]
    public string url { get; set; }
}
