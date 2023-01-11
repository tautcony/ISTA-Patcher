namespace ISTA_Patcher.LicenseManagement.CoreFramework;

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[DesignerCategory("code")]
[XmlType(Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[GeneratedCode("Xsd2Code", "3.4.0.38968")]
[DataContract(Name = "LicensePackage", Namespace = "http://tempuri.org/LicenseInfo.xsd")]
[XmlRoot(Namespace = "http://tempuri.org/LicenseInfo.xsd", IsNullable = true)]
public class LicensePackage
{
	private string packageNameField;

	private string packageVersionField;

	private DateTime packageExpireField;

	private string packageRuleField;

	private static XmlSerializer? serializer;

	[XmlElement(Order = 0)]
	[DataMember]
	public string PackageName { get; set; }

	[XmlElement(Order = 1)]
	[DataMember]
	public string PackageVersion { get; set; }

	[XmlElement(Order = 2)]
	[DataMember]
	public DateTime PackageExpire { get; set; }

	[XmlAttribute]
	[DataMember]
	public string PackageRule { get; set; }

	private static XmlSerializer Serializer
	{
		get
		{
			if (serializer == null)
			{
				serializer = new XmlSerializer(typeof(LicensePackage));
			}
			return serializer;
		}
	}

	public virtual string Serialize()
	{
		using var memoryStream = new MemoryStream();
		Serializer.Serialize(memoryStream, this);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		using var streamReader = new StreamReader(memoryStream);
		return streamReader.ReadToEnd();
	}

	public static bool Deserialize(string licenseXmlContent, out LicensePackage licensePackage, out Exception exception)
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

	public static bool Deserialize(string licenseXmlContent, out LicensePackage licensePackage)
	{
		Exception exception = null;
		return Deserialize(licenseXmlContent, out licensePackage, out exception);
	}

	public static LicensePackage Deserialize(string licenseXmlContent)
	{
		using var stringReader = new StringReader(licenseXmlContent);
		return (LicensePackage) Serializer.Deserialize(XmlReader.Create(stringReader));
	}

	public virtual bool SaveToFile(string fileName, out Exception exception)
	{
		exception = null;
		try
		{
			SaveToFile(fileName);
			return true;
		}
		catch (Exception ex)
		{
			Exception ex2 = (exception = ex);
			return false;
		}
	}

	public virtual void SaveToFile(string fileName)
	{
		var value = Serialize();
		using var streamWriter = new FileInfo(fileName).CreateText();
		streamWriter.WriteLine(value);
	}

	public static bool LoadFromFile(string fileName, out LicensePackage licensePackage, out Exception exception)
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
			Exception ex2 = (exception = ex);
			return false;
		}
	}

	public static bool LoadFromFile(string fileName, out LicensePackage licensePackage)
	{
		Exception exception = null;
		return LoadFromFile(fileName, out licensePackage, out exception);
	}

	public static LicensePackage LoadFromFile(string fileName)
	{
		using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		using var streamReader = new StreamReader(fileStream);
		var license = streamReader.ReadToEnd();
		return Deserialize(license);
	}
}
