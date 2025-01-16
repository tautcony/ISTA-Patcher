// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Utils;

using System.Text;
using ISTAlter.Core;
using ISTAlter.Models.Rheingold.DatabaseProvider;
using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;
using Serilog;

public class RegistryUtils
{
    private static DealerMasterData buildDealerData()
    {
        const string dealerNumber = "AG100";
        string[] brands = [
            "\u0042\u004d\u0057",
            "\u004d\u0069\u006e\u0069",
            "\u0052\u006f\u006c\u006c\u0073\u0052\u006f\u0079\u0063\u0065",
            "\u0042\u004d\u0057\u0069",
            "\u0054\u004f\u0059\u004f\u0054\u0041",
        ];

        List<Contract> contracts =
        [
            new()
            {
                internationalDealerNumber = dealerNumber,
                nationalDealerNumber = dealerNumber,
                startDate = DateTime.UnixEpoch,
                endContractDate = DateTime.MaxValue,
                endServiceDate = DateTime.MaxValue,
                brand = "\u0042\u004d\u0057",
                product = Product.Motorcycle,
                businessLine = BusinessLine.Service,
            },
        ];
        contracts.AddRange(brands.Select(brand => new Contract
        {
            internationalDealerNumber = dealerNumber,
            nationalDealerNumber = dealerNumber,
            startDate = DateTime.UnixEpoch,
            endContractDate = DateTime.MaxValue,
            endServiceDate = DateTime.MaxValue,
            brand = brand,
            product = Product.Vehicle,
            businessLine = BusinessLine.Service,
        }));

        var dealerData = new DealerMasterData
        {
            expirationDate = DateTime.MaxValue,
            hardwareId = "00000000000000000000000000000000",
            verificationCode = "00000000000000000000000000000000",
            distributionPartner = new DistributionPartner
            {
                distributionPartnerNumber = dealerNumber,
                name = "ISTA-Patcher",
                outlet =
                [
                    new Outlet
                    {
                        outletNumber = "01",
                        name = Environment.UserName,
                        protectionVehicleService = true,
                        address = new Address
                        {
                            street1 = "Knorrstraße 147",
                            postalCode = "80939",
                            town1 = "München",
                            country = "DE",
                        },
                        contact = new Communication
                        {
                            email = "ista-patcher@\u0062\u006d\u0077.de",
                            url = Encoding.UTF8.GetString(PatchUtils.Source),
                            voice = new Phone
                            {
                                countryCode = "004989",
                                localNumber = "382-52486",
                            },
                        },
                        businessRelationship = BusinessRelationship.Independent,
                        marketLanguage = ["de-DE", "en-US", "en-GB", "es-ES", "fr-FR", "it-IT", "pl-PL", "cs-CZ", "pt-PT", "tr-TR", "sv-SE", "id-ID", "el-GR", "nl-NL", "ru-RU", "zh-CN", "zh-TW", "ja-JP", "ko-KR", "th-TH"],
                        contract = contracts,
                    },
                ],
            },
        };

        return dealerData;
    }

    public static void GenerateMockRegFile(string basePath, bool force)
    {
        var licenseFile = Path.Join(basePath, "license.reg");
        if (File.Exists(licenseFile) && !force)
        {
            Log.Information("Registry file already exists");
            return;
        }

        var licenseInfo = new LicenseInfo
        {
            Name = "ISTA Patcher",
            Email = "ista-patcher@\u0062\u006d\u0077.de",
            Expiration = DateTime.MaxValue,
            Comment = Encoding.UTF8.GetString(PatchUtils.Source),
            ComputerName = null,
            UserName = "*",
            AvailableBrandTypes = "*",
            AvailableLanguages = "*",
            AvailableOperationModes = "*",
            DistributionPartnerNumber = "*",
            ComputerCharacteristics = [],
            LicenseKey = [],
            LicenseServerURL = null,
            LicenseType = LicenseType.offline,
            SubLicenses = [
                new LicensePackage
                {
                    PackageName = "ForceDealerData",
                    PackageRule = Convert.ToBase64String(DealerMasterData.Serialize(buildDealerData())),
                    PackageExpire = DateTime.MaxValue,
                },
            ],
        };
        var value = licenseInfo.Serialize();
        const string template = "Windows Registry Editor Version 50\n\n[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\\u0042\u004d\u0057Group\\ISPI\\Rheingold]\n\"License\"=\"{}\"";
        File.WriteAllText(licenseFile, template.Replace("{}", ToLiteral(value), StringComparison.Ordinal));
        Log.Information("=== Registry file generated ===");
    }

    private static string ToLiteral(string valueTextForCompiler)
    {
        return valueTextForCompiler
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
