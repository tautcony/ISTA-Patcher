// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Models.Rheingold.DatabaseProvider;

using System.Xml.Serialization;

[XmlType(Namespace = "http://www.\u0062\u006d\u0077.com/ibase/beans/dealerdata")]
[Serializable]
public enum BusinessRelationship
{
    Contract,
    Independent,
}
