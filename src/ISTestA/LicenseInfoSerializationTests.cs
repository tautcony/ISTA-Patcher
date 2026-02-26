// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;

namespace ISTestA;

public class LicenseInfoSerializationTests
{
    [Test]
    public void LicenseInfo_RoundTrip_PreservesKeyFields()
    {
        var source = new LicenseInfo
        {
            Name = "Tester",
            Email = "tester@example.com",
            Expiration = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LicenseType = LicenseType.offline,
            LicenseServerURL = "https://example.invalid/license",
            ComputerCharacteristics = [1, 2, 3],
            LicenseKey = [4, 5, 6],
            SubLicenses =
            [
                new LicensePackage
                {
                    PackageName = "ISTA",
                    PackageVersion = "4.0",
                    PackageExpire = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    PackageRule = "rule",
                },
            ],
        };

        var xml = source.Serialize();
        var roundTrip = LicenseInfo.Deserialize(xml);

        Assert.That(roundTrip.Name, Is.EqualTo(source.Name));
        Assert.That(roundTrip.LicenseType, Is.EqualTo(LicenseType.offline));
        Assert.That(roundTrip.SubLicenses, Is.Not.Null);
        Assert.That(roundTrip.SubLicenses, Has.Count.EqualTo(1));
        Assert.That(roundTrip.SubLicenses![0].PackageName, Is.EqualTo("ISTA"));
    }
}
