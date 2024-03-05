// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Utils.LicenseManagement.CoreFramework;

using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[XmlType(Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[XmlRoot(Namespace = "http://tempuri.org/LicenseInfo.xsd", IsNullable = true)]
[DataContract(Name = "LicensePackage", Namespace = "http://tempuri.org/LicenseInfo.xsd")]
public class LicensePackage : EntitySerializer<LicensePackage>
{
    [XmlElement(Order = 0)]
    [DataMember]
    public string? PackageName { get; set; }

    [XmlElement(Order = 1)]
    [DataMember]
    public string? PackageVersion { get; set; }

    [XmlElement(Order = 2)]
    [DataMember]
    public DateTime PackageExpire { get; set; }

    [XmlAttribute]
    [DataMember]
    public string? PackageRule { get; set; }
}
