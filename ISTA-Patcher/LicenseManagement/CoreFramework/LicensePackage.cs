// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.LicenseManagement.CoreFramework;

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[XmlType(Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[XmlRoot(Namespace = "http://tempuri.org/LicenseInfo.xsd", IsNullable = true)]
[DataContract(Name = "LicensePackage", Namespace = "http://tempuri.org/LicenseInfo.xsd")]
public class LicensePackage
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

    private static XmlSerializer Serializer { get; set; } = new(typeof(LicensePackage));

    public virtual string Serialize()
    {
        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, this);
        memoryStream.Seek(0L, SeekOrigin.Begin);
        using var streamReader = new StreamReader(memoryStream);
        return streamReader.ReadToEnd();
    }

    public static bool Deserialize(string licenseXmlContent, out LicensePackage? licensePackage, out Exception? exception)
    {
        exception = null;
        licensePackage = null;
        try
        {
            licensePackage = Deserialize(licenseXmlContent);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    public static bool Deserialize(string licenseXmlContent, out LicensePackage? licensePackage)
    {
        return Deserialize(licenseXmlContent, out licensePackage, out _);
    }

    public static LicensePackage? Deserialize(string licenseXmlContent)
    {
        using var stringReader = new StringReader(licenseXmlContent);
        return (LicensePackage?)Serializer.Deserialize(XmlReader.Create(stringReader));
    }

    public virtual bool SaveToFile(string fileName, out Exception? exception)
    {
        exception = null;
        try
        {
            this.SaveToFile(fileName);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    public virtual void SaveToFile(string fileName)
    {
        var value = this.Serialize();
        using var streamWriter = new FileInfo(fileName).CreateText();
        streamWriter.WriteLine(value);
    }

    public static bool LoadFromFile(string fileName, out LicensePackage? licensePackage, out Exception? exception)
    {
        exception = null;
        licensePackage = null;
        try
        {
            licensePackage = LoadFromFile(fileName);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    public static bool LoadFromFile(string fileName, out LicensePackage licensePackage)
    {
        return LoadFromFile(fileName, out licensePackage, out _);
    }

    public static LicensePackage? LoadFromFile(string fileName)
    {
        using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);
        var license = streamReader.ReadToEnd();
        return Deserialize(license);
    }
}
