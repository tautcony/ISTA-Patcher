// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAlter.Models.Rheingold.LicenseManagement;
using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;

public class LicenseSerializationTests
{
    private string _tempPath = null!;

    [SetUp]
    public void Setup()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "ISTATest_License_" + Guid.NewGuid().ToString("N") + ".xml");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempPath))
        {
            File.Delete(_tempPath);
        }
    }

    [Test]
    public void LicenseInfoSerializer_ToStringAndFromString_RoundTripsLicenseInfo()
    {
        var license = CreateLicense();

        var xml = LicenseInfoSerializer.ToString(license);
        var roundTripped = LicenseInfoSerializer.FromString<LicenseInfo>(xml);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(xml, Does.StartWith("<?xml version=\"1.0\"?>\r\n"));
            Assert.That(roundTripped, Is.Not.Null);
            Assert.That(roundTripped!.Name, Is.EqualTo(license.Name));
            Assert.That(roundTripped.Email, Is.EqualTo(license.Email));
            Assert.That(roundTripped.ComputerCharacteristics, Is.EqualTo(license.ComputerCharacteristics));
            Assert.That(roundTripped.SubLicenses, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void LicenseInfoSerializer_InvalidXml_ReturnsNull()
    {
        var result = LicenseInfoSerializer.FromString<LicenseInfo>("<not-valid>");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void EntitySerializer_SerializeDeserializeAndClone_RoundTripValues()
    {
        var license = CreateLicense();

        var xml = license.Serialize();
        var success = LicenseInfo.Deserialize(xml, out LicenseInfo? roundTripped, out var exception);
        var clone = (LicenseInfo)license.Clone();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(exception, Is.Null);
            Assert.That(roundTripped, Is.Not.Null);
            Assert.That(roundTripped!.Name, Is.EqualTo(license.Name));
            Assert.That(clone, Is.Not.SameAs(license));
            Assert.That(clone.SubLicenses, Is.Not.SameAs(license.SubLicenses));
            Assert.That(clone.ComputerCharacteristics, Is.Not.SameAs(license.ComputerCharacteristics));
            Assert.That(clone.LicenseKey, Is.Not.SameAs(license.LicenseKey));
        }
    }

    [Test]
    public void EntitySerializer_InvalidXml_ReturnsFalseWithException()
    {
        var success = LicenseInfo.Deserialize("<bad>", out LicenseInfo? data, out var exception);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(data, Is.Null);
            Assert.That(exception, Is.Not.Null);
        }
    }

    [Test]
    public void EntitySerializer_SaveAndLoadFromFile_RoundTripsContent()
    {
        var license = CreateLicense();

        license.SaveToFile(_tempPath);
        var loaded = LicenseInfo.LoadFromFile(_tempPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(_tempPath), Is.True);
            Assert.That(loaded.Name, Is.EqualTo(license.Name));
            Assert.That(loaded.SubLicenses, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void EntitySerializer_LoadAndSaveBooleanOverloads_ReportErrors()
    {
        var loadSuccess = LicenseInfo.LoadFromFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), out LicenseInfo? data, out var loadException);
        var saveSuccess = CreateLicense().SaveToFile(Path.Combine(_tempPath, "missing-dir", "license.xml"), out var saveException);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(loadSuccess, Is.False);
            Assert.That(data, Is.Null);
            Assert.That(loadException, Is.Not.Null);
            Assert.That(saveSuccess, Is.False);
            Assert.That(saveException, Is.Not.Null);
        }
    }

    private static LicenseInfo CreateLicense()
    {
        return new LicenseInfo
        {
            Name = "Tester",
            Email = "tester@example.com",
            Expiration = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Comment = "unit test",
            ComputerName = "TEST-PC",
            UserName = "tester",
            AvailableBrandTypes = "BMW",
            AvailableLanguages = "en-US",
            AvailableOperationModes = "offline",
            DistributionPartnerNumber = "12345",
            LicenseServerURL = "https://example.com",
            ComputerCharacteristics = [1, 2, 3],
            LicenseKey = [4, 5, 6],
            LicenseType = LicenseType.offline,
            SubLicenses =
            [
                new LicensePackage
                {
                    PackageName = "Base",
                    PackageVersion = "1.0",
                    PackageExpire = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    PackageRule = "Allow",
                },
            ],
        };
    }
}
