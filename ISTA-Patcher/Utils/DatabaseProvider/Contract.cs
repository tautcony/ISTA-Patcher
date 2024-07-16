// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Utils.DatabaseProvider;

using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[XmlRoot(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata", IsNullable = true)]
[DataContract(Name = "Contract", Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public class Contract
{
    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    [DataMember]
    public string? brand { get; set; }

    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    [DataMember]
    public Product product { get; set; }

    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    [DataMember]
    public BusinessLine businessLine { get; set; }

    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    [DataMember]
    public string? salesBranch { get; set; }

    [XmlAttribute]
    [DataMember]
    public string? internationalDealerNumber { get; set; }

    [XmlAttribute]
    [DataMember]
    public string? nationalDealerNumber { get; set; }

    [XmlAttribute]
    [DataMember]
    public DateTime startDate { get; set; }

    [XmlAttribute]
    [DataMember]
    public DateTime endContractDate { get; set; }

    [XmlIgnore]
    [DataMember]
    public bool endContractDateSpecified { get; set; }

    [XmlAttribute]
    [DataMember]
    public DateTime endServiceDate { get; set; }

    [XmlIgnore]
    [DataMember]
    public bool endServiceDateSpecified { get; set; }

    [XmlAttribute]
    [DataMember]
    public bool mobileService { get; set; }

    [XmlIgnore]
    [DataMember]
    public bool mobileServiceSpecified { get; set; }
}
