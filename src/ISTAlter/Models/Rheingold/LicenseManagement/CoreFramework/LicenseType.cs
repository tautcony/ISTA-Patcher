// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;

using System.Xml.Serialization;

[Serializable]
[XmlType(Namespace = "http://tempuri.org/LicenseInfo.xsd")]
public enum LicenseType
{
    offline,
    online,
}
