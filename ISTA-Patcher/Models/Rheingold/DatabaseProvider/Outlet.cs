// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Models.Rheingold.DatabaseProvider;

using System;
using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = true)]
[DataContract(Name = "Outlet", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class Outlet
{
    public Outlet()
    {
        this.contract = [];
        this.marketLanguage = [];
        this.contact = new Communication();
        this.address = new Address();
    }

    [XmlElement(Form = XmlSchemaForm.Unqualified, Order = 0)]
    [DataMember]
    public Address address { get; set; }

    [XmlElement(Form = XmlSchemaForm.Unqualified, Order = 1)]
    [DataMember]
    public Communication contact { get; set; }

    [XmlElement(Form = XmlSchemaForm.Unqualified, Order = 2)]
    [DataMember]
    public BusinessRelationship businessRelationship { get; set; }

    [XmlElement("marketLanguage", Form = XmlSchemaForm.Unqualified, DataType = "language", Order = 3)]
    [DataMember]
    public List<string> marketLanguage { get; set; }

    [XmlElement("contract", Form = XmlSchemaForm.Unqualified, Order = 4)]
    [DataMember]
    public List<Contract> contract { get; set; }

    [XmlAttribute(DataType = "integer")]
    [DataMember]
    public string outletNumber { get; set; }

    [XmlAttribute]
    [DataMember]
    public string name { get; set; }

    [XmlAttribute]
    [DataMember]
    public bool protectionVehicleService { get; set; }
}
