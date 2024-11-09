// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;

using System.Runtime.Serialization;
using System.Xml.Serialization;

[Serializable]
[XmlRoot(Namespace = "http://tempuri.org/LicenseInfo.xsd", IsNullable = false)]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[DataContract(Name = "LicenseInfo", Namespace = "http://tempuri.org/LicenseInfo.xsd")]
public class LicenseInfo : EntitySerializer<LicenseInfo>, ICloneable
{
    [XmlElement(IsNullable = true, Order = 0)]
    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    [XmlElement(IsNullable = true, Order = 1)]
    public string? Email { get; set; }

    [DataMember]
    [XmlElement(Order = 2)]
    public DateTime Expiration { get; set; }

    [XmlElement(IsNullable = true, Order = 3)]
    [DataMember]
    public string? Comment { get; set; }

    [DataMember]
    [XmlElement(IsNullable = true, Order = 4)]
    public string? ComputerName { get; set; }

    [DataMember]
    [XmlElement(IsNullable = true, Order = 5)]
    public string? UserName { get; set; }

    [DataMember]
    [XmlElement(IsNullable = true, Order = 6)]
    public string? AvailableBrandTypes { get; set; }

    [DataMember]
    [XmlElement(IsNullable = true, Order = 7)]
    public string? AvailableLanguages { get; set; }

    [XmlElement(IsNullable = true, Order = 8)]
    [DataMember]
    public string? AvailableOperationModes { get; set; }

    [XmlElement(IsNullable = true, Order = 9)]
    [DataMember]
    public string? DistributionPartnerNumber { get; set; }

    [DataMember]
    [XmlElement(IsNullable = true, Order = 12)]
    public string? LicenseServerURL { get; set; }

    [DataMember]
    [XmlElement(DataType = "base64Binary", IsNullable = true, Order = 10)]
    public byte[]? ComputerCharacteristics { get; set; }

    [DataMember]
    [XmlElement(DataType = "base64Binary", IsNullable = true, Order = 11)]
    public byte[]? LicenseKey { get; set; }

    [XmlElement(Order = 13)]
    [DataMember]
    public LicenseType LicenseType { get; set; } = LicenseType.offline;

    [DataMember]
    [XmlElement("SubLicenses", Order = 14)]
    public List<LicensePackage>? SubLicenses { get; set; }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
