// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Core;

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using ISTA_Patcher.Utils.DatabaseProvider;
using ISTA_Patcher.Utils.LicenseManagement.CoreFramework;
using Serilog;

/// <summary>
/// A utility class for patching files and directories.
/// Contains helper functions and variables.
/// </summary>
internal static partial class PatchUtils
{
    private static readonly string Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.CurrentCulture);
    private static readonly ModuleContext ModCtx = new(TheAssemblyResolver.Instance);
    private static readonly IDeobfuscatorContext DeobfuscatorContext = new DeobfuscatorContext();
    private static readonly NewProcessAssemblyClientFactory ProcessAssemblyClientFactory = new();

    private static string Version
    {
        get
        {
            var infoVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (infoVersion != null)
            {
                var match = VersionPattern().Match(infoVersion.InformationalVersion);
                if (!match.Success)
                {
                    return infoVersion.InformationalVersion;
                }

                var shortHash = match.Groups["hash"].Value[..7];
                return $"{match.Groups["version"].Value}+{shortHash}";
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
            return version.ToString();
        }
    }

    public static string Config => Encoding.UTF8.GetString([
        0x50, 0x6f, 0x77, 0x65, 0x72, 0x65, 0x64, 0x20, 0x62, 0x79, 0x20, 0x49, 0x53, 0x54, 0x41, 0x2d, 0x50, 0x61, 0x74,
        0x63, 0x68, 0x65, 0x72, 0x20,
    ]) + Version;

    public static byte[] Source => [
        0x68, 0x74, 0x74, 0x70, 0x73, 0x3a, 0x2f, 0x2f, 0x67, 0x69, 0x74, 0x68, 0x75, 0x62, 0x2e, 0x63, 0x6f, 0x6d,
        0x2f, 0x74, 0x61, 0x75, 0x74, 0x63, 0x6f, 0x6e, 0x79, 0x2f, 0x49, 0x53, 0x54, 0x41, 0x2d, 0x50, 0x61, 0x74,
        0x63, 0x68, 0x65, 0x72,
    ];

    /// <summary>
    /// Loads a module from the specified file.
    /// </summary>
    /// <param name="fileName">The path to the module file.</param>
    /// <returns>The loaded <see cref="ModuleDefMD"/>.</returns>
    public static ModuleDefMD LoadModule(string fileName)
    {
        var options = new ModuleCreationOptions(ModCtx) { TryToLoadPdbFromDisk = false };
        var module = ModuleDefMD.Load(fileName, options);
        return module;
    }

    /// <summary>
    /// Saves the given assembly module to a file with the specified filename.
    /// </summary>
    /// <param name="module">The <see cref="ModuleDef"/> to be saved.</param>
    /// <param name="newFilename">The path to the module file will be saved.</param>
    public static void SaveModule(ModuleDefMD module, string newFilename)
    {
        if (module.IsILOnly)
        {
            var writerOptions = new ModuleWriterOptions(module);
            module.Write(newFilename, writerOptions);
        }
        else
        {
            var writerOptions = new NativeModuleWriterOptions(module, optimizeImageSize: true)
            {
                KeepExtraPEData = true,
                KeepWin32Resources = true,
            };
            module.NativeWrite(newFilename, writerOptions);
        }
    }

    /// <summary>
    /// Applies a patch to a method in the specified assembly.
    /// </summary>
    /// <param name="module">The <see cref="ModuleDefMD"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="desc">The description of the method.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the function applying the patch.</param>
    /// <returns>The number of functions patched.</returns>
    private static int PatchFunction(
        this ModuleDefMD module,
        string type,
        string name,
        string desc,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var function = module.GetMethod(type, name, desc);
        Log.Verbose("Applying patch {PatchName} => {Name} <= {Module}: {Result}", memberName, name, module, function != null);
        if (function == null)
        {
            return 0;
        }

        operation(function);
        return 1;
    }

    /// <summary>
    /// Applies a patch to a property getter in the specified assembly.
    /// </summary>
    /// <param name="module">The <see cref="ModuleDefMD"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the function applying the patch.</param>
    /// <returns>The number of functions patched.</returns>
    private static int PatcherGetter(
        this ModuleDefMD module,
        string type,
        string propertyName,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var typeDef = module.GetType(type);
        if (typeDef == null)
        {
            return 0;
        }

        var propertyDef = DnlibUtils.FindPropertyInClassAndBaseClasses(typeDef, propertyName);
        if (propertyDef?.GetMethod == null)
        {
            return 0;
        }

        Log.Verbose("Applying patch {PatchName} => {Name} <= {Module}: {Result}", memberName, $"{propertyName}::getter", module, propertyDef.GetMethod != null);

        operation(propertyDef.GetMethod);
        return 1;
    }

    /// <summary>
    /// Check if the assembly is patched by this patcher.
    /// </summary>
    /// <param name="module">module to check.</param>
    /// <returns>ture for assembly has been patched.</returns>
    public static string? HavePatchedMark(ModuleDefMD module)
    {
        var patchedType = module.GetType("Patched.By.TC");
        if (patchedType == null)
        {
            return null;
        }

        var field = patchedType.Fields.FirstOrDefault(field => field.Name == "version");
        var version = field?.Constant.Value as string;
        return version;
    }

    /// <summary>
    /// Set the patched mark to the assembly.
    /// </summary>
    /// <param name="module">module to set.</param>
    public static void SetPatchedMark(ModuleDefMD module)
    {
        if (HavePatchedMark(module) != null)
        {
            return;
        }

        var patchedType = new TypeDefUser(
            "Patched.By",
            "TC",
            module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.NestedPrivate,
        };
        var dateField = new FieldDefUser(
            "date",
            new FieldSig(module.CorLibTypes.String),
            dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Timestamp),
        };
        var urlField = new FieldDefUser(
            "repo",
            new FieldSig(module.CorLibTypes.String),
            dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Encoding.UTF8.GetString(Source)),
        };

        var versionField = new FieldDefUser(
            "version",
            new FieldSig(module.CorLibTypes.String),
            dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Version),
        };

        patchedType.Fields.Add(dateField);
        patchedType.Fields.Add(urlField);
        patchedType.Fields.Add(versionField);
        module.Types.Add(patchedType);

        var description = module.Assembly.CustomAttributes.FirstOrDefault(attribute =>
            attribute.AttributeType.Name == nameof(AssemblyDescriptionAttribute));
        if (description is { HasConstructorArguments: true })
        {
            description.ConstructorArguments[0] = new CAArgument(module.CorLibTypes.String, Config);
        }
    }

    /// <summary>
    /// Performs deobfuscation on an obfuscated file and saves the result to a new file.
    /// </summary>
    /// <param name="fileName">The path to the obfuscated file.</param>
    /// <param name="newFileName">The path to save the deobfuscated file.</param>
    public static void DeObfuscation(string fileName, string newFileName)
    {
        var deobfuscatorInfo = new DeobfuscatorInfo();

        using var file = new ObfuscatedFile(
            new ObfuscatedFile.Options
            {
                ControlFlowDeobfuscation = true,
                Filename = fileName,
                NewFilename = newFileName,
                StringDecrypterType = DecrypterType.Static,
            },
            ModCtx,
            ProcessAssemblyClientFactory)
        {
            DeobfuscatorContext = DeobfuscatorContext,
        };

        file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
        file.DeobfuscateBegin();
        file.Deobfuscate();
        file.DeobfuscateEnd();
        file.Save();
    }

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
            new Contract
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
                            url = Encoding.UTF8.GetString(Source),
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
            Comment = Encoding.UTF8.GetString(Source),
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
        const string template = "Windows Registry Editor Version 5.00\n\n[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\\u0042\u004d\u0057Group\\ISPI\\Rheingold]\n\"License\"=\"{}\"";
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

    [GeneratedRegex(@"(?<version>\d+\.\d+\.\d+)\+(?<hash>[a-f0-9]+)", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionPattern();
}
