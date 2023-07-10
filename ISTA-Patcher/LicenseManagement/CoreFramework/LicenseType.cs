// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.LicenseManagement.CoreFramework;

using System;
using System.Xml.Serialization;

[Serializable]
[XmlType(Namespace = "http://tempuri.org/LicenseInfo.xsd")]
public enum LicenseType
{
    offline,
    online,
}
