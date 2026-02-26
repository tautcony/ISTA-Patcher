// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using System.Text;
using System.Xml.Serialization;
using ISTAlter.Models.Rheingold.DatabaseProvider;

namespace ISTestA;

public class RheingoldDatabaseProviderSerializationTests
{
    [Test]
    public void DealerMasterData_RoundTrip_PreservesRepresentativeFields()
    {
        var model = new DealerMasterData
        {
            expirationDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            hardwareId = "HW-ABC",
            verificationCode = "VER-123",
            distributionPartner = new DistributionPartner
            {
                distributionPartnerNumber = "11451",
                name = "PartnerName",
                outlet =
                [
                    new Outlet
                    {
                        outletNumber = "01",
                        name = "OutletName",
                        protectionVehicleService = true,
                        marketLanguage = ["en"],
                        businessRelationship = BusinessRelationship.Contract,
                        address = new Address
                        {
                            street1 = "Road 1",
                            postalCode = "10000",
                            country = "DE",
                        },
                        contact = new Communication
                        {
                            email = "dealer@example.com",
                            voice = new Phone { countryCode = "+49", areaCode = "89", localNumber = "123456" },
                            fax = new Phone { countryCode = "+49", areaCode = "89", localNumber = "654321" },
                        },
                        contract =
                        [
                            new Contract
                            {
                                brand = "BMW",
                                product = Product.Vehicle,
                                businessLine = BusinessLine.Service,
                                startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                endContractDate = new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                endContractDateSpecified = true,
                                endServiceDate = new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                endServiceDateSpecified = true,
                                mobileService = true,
                                mobileServiceSpecified = true,
                            },
                        ],
                    },
                ],
            },
        };

        var bytes = DealerMasterData.Serialize(model);
        var xml = Encoding.UTF8.GetString(bytes);
        Assert.That(xml, Does.Contain("distributionPartnerNumber=\"11451\""));
        Assert.That(xml, Does.Contain("outletNumber=\"01\""));

        var serializer = new XmlSerializer(typeof(DealerMasterData));
        using var stream = new MemoryStream(bytes);
        var roundTrip = (DealerMasterData?)serializer.Deserialize(stream);

        Assert.That(roundTrip, Is.Not.Null);
        Assert.That(roundTrip!.distributionPartner.outlet, Has.Count.EqualTo(1));
        Assert.That(roundTrip.distributionPartner.outlet[0].contract, Has.Count.EqualTo(1));
        Assert.That(roundTrip.distributionPartner.outlet[0].contract[0].mobileServiceSpecified, Is.True);
        Assert.That(roundTrip.distributionPartner.outlet[0].contract[0].mobileService, Is.True);
    }
}
