// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable InconsistentNaming
namespace ISTA_Patcher.Utils.LicenseManagement.CoreFramework;

using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[XmlRoot(Namespace = "http://tempuri.org/LicenseInfo.xsd", IsNullable = false)]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[DataContract(Name = "LicenseInfo", Namespace = "http://tempuri.org/LicenseInfo.xsd")]
public class LicenseInfo : ICloneable
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
    public List<LicensePackage> SubLicenses { get; set; }

    private static XmlSerializer Serializer { get; } = new(typeof(LicenseInfo));

    public virtual string Serialize()
    {
        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, this);
        memoryStream.Seek(0L, SeekOrigin.Begin);
        using var streamReader = new StreamReader(memoryStream);
        return streamReader.ReadToEnd();
    }

    public static bool Deserialize(string licenseXmlContent, out LicenseInfo licenseInfo, out Exception? exception)
    {
        exception = null;
        licenseInfo = null;
        try
        {
            licenseInfo = Deserialize(licenseXmlContent);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    public static bool Deserialize(string licenseXmlContent, out LicenseInfo licenseInfo)
    {
        return Deserialize(licenseXmlContent, out licenseInfo, out _);
    }

    public static LicenseInfo Deserialize(string licenseXmlContent)
    {
        StringReader stringReader = null;
        try
        {
            stringReader = new StringReader(licenseXmlContent);
            return (LicenseInfo)Serializer.Deserialize(XmlReader.Create(stringReader)) ?? throw new InvalidOperationException();
        }
        finally
        {
            stringReader?.Dispose();
        }
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
        using var streamWriter = new FileInfo(fileName).CreateText();
        var value = this.Serialize();
        streamWriter.WriteLine(value);
    }

    public static bool LoadFromFile(string fileName, out LicenseInfo licenseInfo, out Exception? exception)
    {
        exception = null;
        licenseInfo = null;
        try
        {
            licenseInfo = LoadFromFile(fileName);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    public static bool LoadFromFile(string fileName, out LicenseInfo licenseInfo)
    {
        return LoadFromFile(fileName, out licenseInfo, out _);
    }

    public static LicenseInfo LoadFromFile(string fileName)
    {
        using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);
        var licenseContent = streamReader.ReadToEnd();
        return Deserialize(licenseContent);
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
