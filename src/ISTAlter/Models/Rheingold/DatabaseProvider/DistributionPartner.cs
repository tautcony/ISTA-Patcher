// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Models.Rheingold.DatabaseProvider;

using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = false)]
[DataContract(Name = "DistributionPartner", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class DistributionPartner
{
    public DistributionPartner()
    {
        this.outlet = [];
    }

    [XmlElement("outlet", Form = XmlSchemaForm.Unqualified, Order = 0)]
    [DataMember]
    public List<Outlet> outlet { get; set; }

    [XmlAttribute]
    [DataMember]
    public string distributionPartnerNumber { get; set; }

    [XmlAttribute]
    [DataMember]
    public string name { get; set; }
}
