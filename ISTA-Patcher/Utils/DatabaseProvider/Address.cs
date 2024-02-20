// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Utils.DatabaseProvider;

using System.Runtime.Serialization;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = true)]
[DataContract(Name = "Address", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class Address
{
    [XmlAttribute]
    [DataMember]
    public string street1 { get; set; }

    [XmlAttribute]
    [DataMember]
    public string street2 { get; set; }

    [XmlAttribute]
    [DataMember]
    public string postalCode { get; set; }

    [XmlAttribute]
    [DataMember]
    public string town1 { get; set; }

    [XmlAttribute]
    [DataMember]
    public string town2 { get; set; }

    [XmlAttribute]
    [DataMember]
    public string country { get; set; }
}
