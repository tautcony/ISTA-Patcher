namespace ISTA_Patcher.LicenseManagement.CoreFramework;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[GeneratedCode("Xsd2Code", "3.4.0.38968")]
[DesignerCategory("code")]
[XmlRoot(Namespace = "http://tempuri.org/LicenseInfo.xsd", IsNullable = false)]
[DataContract(Name = "LicenseInfo", Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/LicenseInfo.xsd")]
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
	public byte[]? ComputerCharacteristics
	{
		get => computerCharacteristicsField;
		set => computerCharacteristicsField = value;
	}

	[DataMember]
	[XmlElement(DataType = "base64Binary", IsNullable = true, Order = 11)]
	public byte[]? LicenseKey
	{
		get => licenseKeyField;
		set => licenseKeyField = value;
	}

	[XmlElement(Order = 13)]
	[DataMember]
	public LicenseType LicenseType
	{
		get => licenseTypeField;
		set => licenseTypeField = value;
	}

	[DataMember]
	[XmlElement("SubLicenses", Order = 14)]
	public List<LicensePackage> SubLicenses
	{
		get => subLicensesField;
		set => subLicensesField = value;
	}

	private static XmlSerializer Serializer => serializer ??= new XmlSerializer(typeof(LicenseInfo));

	public LicenseInfo()
	{
		licenseTypeField = LicenseType.offline;
	}

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
			SaveToFile(fileName);
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
		var value = Serialize();
		streamWriter.WriteLine(value);
		streamWriter.Close();
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
		streamReader.Close();
		fileStream.Close();
		return Deserialize(licenseContent);
	}

	public object Clone()
	{
		return MemberwiseClone();
	}

	private byte[]? computerCharacteristicsField;

	private byte[]? licenseKeyField;

	private LicenseType licenseTypeField;

	private List<LicensePackage> subLicensesField;

	private static XmlSerializer? serializer;
}
