// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Utils.DatabaseProvider;

using System.Runtime.Serialization;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = true)]
[DataContract(Name = "Phone", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class Phone
{
    [XmlAttribute]
    [DataMember]
    public string? countryCode { get; set; }

    [XmlAttribute]
    [DataMember]
    public string? areaCode { get; set; }

    [XmlAttribute]
    [DataMember]
    public string? localNumber { get; set; }
}
