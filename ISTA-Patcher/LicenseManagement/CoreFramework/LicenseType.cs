namespace ISTA_Patcher.LicenseManagement.CoreFramework;

using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

[Serializable]
[XmlType(Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[GeneratedCode("Xsd2Code", "3.4.0.38968")]
public enum LicenseType
{
    offline,
    online
}
