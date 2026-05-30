// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Utils;

using System.Text;
using ISTAlter.Core;
using ISTAlter.Models.Rheingold.DatabaseProvider;
using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;
using Serilog;

public static class RegistryUtils
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
            Log.Information("Registry file already exists: {Path}", licenseFile);
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
        const string template = "Windows Registry Editor Version 5.00\n\n[HKEY_LOCAL_MACHINE\\SOFTWARE\\\u0042\u004d\u0057Group\\ISPI\\Rheingold]\n\"License\"=\"{}\"";

        // The template targets the native 64-bit hive by default (ISTA >= 4.55).
        // Older 32-bit versions (< 4.55) need the WOW6432Node redirection node.
        var registryTemplate = IsSixtyFourBit(basePath)
            ? template
            : template.Replace("SOFTWARE\\", "SOFTWARE\\WOW6432Node\\", StringComparison.Ordinal);
        File.WriteAllText(licenseFile, registryTemplate.Replace("{}", ToLiteral(value), StringComparison.Ordinal));
        Log.Information("Registry file generated: {Path}", licenseFile);
    }

    private static bool IsSixtyFourBit(string basePath)
    {
        var version = GetIstaVersion(basePath);

        // Default to the 64-bit hive; only fall back to WOW6432Node when the ISTA
        // version is positively known to predate the 4.55 64-bit switch.
        var sixtyFourBit = version == null || version >= new Version("4.55");
        Log.Information(
            "ISTA version {Version} detected, generating {Hive} registry key",
            version?.ToString() ?? "unknown",
            sixtyFourBit ? "64-bit" : "WOW6432Node (32-bit)");
        return sixtyFourBit;
    }

    private static Version? GetIstaVersion(string basePath)
    {
        // RheingoldCoreFramework.dll carries the ISTA version and is always present
        // (it hosts the mandatory license patches), so it is the canonical source.
        var corePath = Path.Join(basePath, "RheingoldCoreFramework.dll");
        if (!File.Exists(corePath))
        {
            Log.Warning("Cannot determine ISTA version: {Path} not found", corePath);
            return null;
        }

        try
        {
            var module = PatchUtils.LoadModule(corePath);
            return module.Assembly?.Version;
        }
        catch (Exception ex)
        {
            Log.Warning("Cannot determine ISTA version from {Path}: {Message}", corePath, ex.Message);
            return null;
        }
    }

    private static string ToLiteral(string valueTextForCompiler)
    {
        return valueTextForCompiler
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
