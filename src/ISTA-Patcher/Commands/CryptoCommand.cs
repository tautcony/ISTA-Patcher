// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Commands;

using DotMake.CommandLine;
using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Utils;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Serilog;
using Spectre.Console;

[CliCommand(
    Name = "crypto",
    Description = "Perform cryptographic operations.",
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormAutoGenerate = false,
    Parent = typeof(RootCommand)
)]
public class CryptoCommand
{
    public RootCommand? ParentCommand { get; set; }

    [CliOption(Description = "Decrypt the integrity checklist.")]
    public bool Decrypt { get; set; }

    [CliOption(Description = "Verify the integrity of the checklist.")]
    public bool Integrity { get; set; }

    [CliArgument(Description = "Specify the path for ISTA-P.", Required = true)]
    public string? TargetPath { get; set; }

    [CliOption(Description = "Create a key pair.")]
    public bool CreateKeyPair { get; set; }

    public void Run()
    {
        var opts = new ISTAOptions.CryptoOptions
        {
            Verbosity = this.ParentCommand!.Verbosity,
            Decrypt = this.Decrypt,
            Integrity = this.Integrity,
            CreateKeyPair = this.CreateKeyPair,
            TargetPath = this.TargetPath,
        };

        Execute(opts).Wait();
    }

    public static async Task<int> Execute(ISTAOptions.CryptoOptions opts)
    {
        using var transaction = new TransactionHandler("ISTA-Patcher", "crypto");
        opts.Transaction = transaction;
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;
        var basePath = Path.Join(opts.TargetPath, ISTAlter.Utils.Constants.TesterGUIPath[0]);

        if (opts.CreateKeyPair)
        {
            using var child = new SpanHandler(transaction, "CreateKeyPair");
            var filePath = Path.Join(basePath, "keyContainer.pfx");
            await using Stream val = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            GenerateKeyStore().Save(val, KeyPairConfig.Select(i => (char)i).ToArray(), new SecureRandom());
            Log.Information("Key pair saved to {FilePath}", filePath);
            return 0;
        }

        if (opts.Decrypt)
        {
            using var child = new SpanHandler(transaction, "Decrypt");
            var encryptedFileList = ISTAlter.Utils.Constants.EncCnePath.Aggregate(opts.TargetPath, Path.Join);
            return await LoadFileList(encryptedFileList, opts.Integrity, basePath);
        }

        Log.Warning("No operation matched, exiting...");
        return -1;
    }

    private static async Task<int> LoadFileList(string encryptedFileList, bool integrity, string basePath)
    {
        if (!File.Exists(encryptedFileList))
        {
            Log.Error("File {FilePath} does not exist", encryptedFileList);
            return -1;
        }

        var fileList = IntegrityUtils.DecryptFile(encryptedFileList);
        if (fileList == null)
        {
            return -1;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn(new TableColumn("[u]FilePath[/]").NoWrap())
            .AddColumn(new TableColumn("[u]Hash(SHA256)[/]").NoWrap())
            .AddColumn(new TableColumn("[u]Integrity[/]").NoWrap());

        foreach (var fileInfo in fileList)
        {
            if (integrity)
            {
                var checkResult = await CheckFileIntegrity(basePath, fileInfo).ConfigureAwait(false);
                var info = string.IsNullOrEmpty(checkResult.Value)
                    ? fileInfo.FilePath
                    : $"{fileInfo.FilePath} ({checkResult.Value})";
                table.AddRow(info, fileInfo.Hash, checkResult.Key);
            }
            else
            {
                table.AddRow(fileInfo.FilePath, fileInfo.Hash, "/");
            }
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private static async Task<KeyValuePair<string, string>> CheckFileIntegrity(string basePath, HashFileInfo fileInfo)
    {
        const string checkNG = "[red]NG[/]";
        const string checkNF = "[yellow]404[/]";
        const string checkOK = "[green]OK[/]";
        const string checkEmpty = "[gray]???[/]";
        const string checkSignNG = "|[red]SIGN:NG[/]";
        const string checkSignNF = "|[yellow]SIGN:404[/]";
        const string checkSignOK = "|[green]SIGN:OK[/]";

        string? checkResult;
        string version;
        var filePath = Path.Join(basePath, fileInfo.FilePath);
        if (!File.Exists(filePath))
        {
            return new KeyValuePair<string, string>(checkNF, string.Empty);
        }

        try
        {
            var module = PatchUtils.LoadModule(filePath);
            version = module.Assembly.Version.ToString();
        }
        catch (System.BadImageFormatException)
        {
            version = "Native";
        }

        if (fileInfo.Hash == string.Empty)
        {
            checkResult = checkEmpty;
        }
        else
        {
            var realHash = await HashFileInfo.CalculateHash(filePath).ConfigureAwait(false);
            checkResult = string.Equals(realHash, fileInfo.Hash, StringComparison.Ordinal) ? checkOK : checkNG;
        }

        if (OperatingSystem.IsWindows())
        {
            var wasVerified = false;

            var bChecked = NativeMethods.StrongNameSignatureVerificationEx(filePath, fForceVerification: true, ref wasVerified);
            if (bChecked)
            {
                checkResult += wasVerified ? checkSignOK : checkSignNG;
            }
            else
            {
                checkResult += checkSignNF;
            }
        }

        return new KeyValuePair<string, string>(checkResult, version);
    }

    private static readonly byte[] KeyPairConfig = [0x47, 0x23, 0x38, 0x78, 0x21, 0x39, 0x73, 0x44, 0x32, 0x40, 0x71, 0x5a, 0x36, 0x26, 0x6c, 0x46, 0x31];

    private static Pkcs12Store GenerateKeyStore()
    {
        var keyPairGenerator = new ECKeyPairGenerator("ECDSA");
        keyPairGenerator.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP384r1, new SecureRandom()));

        var keyPair = keyPairGenerator.GenerateKeyPair();
        var cert = GenerateCertificate(keyPair);
        var store = new Pkcs12StoreBuilder().Build();

        store.SetKeyEntry(
            "alias",
            new AsymmetricKeyEntry(keyPair.Private),
            [new X509CertificateEntry(cert)]);
        return store;
    }

    private static X509Certificate GenerateCertificate(AsymmetricCipherKeyPair keyPair)
    {
        var certificateGenerator = new X509V3CertificateGenerator();
        certificateGenerator.SetSerialNumber(BigInteger.ValueOf(1));
        certificateGenerator.SetIssuerDN(new X509Name("CN=ISTA-Patcher"));
        certificateGenerator.SetSubjectDN(new X509Name("CN=ISTA-Patcher"));
        certificateGenerator.SetNotBefore(DateTime.UtcNow.Date.AddYears(-114));
        certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(514));
        certificateGenerator.SetPublicKey(keyPair.Public);
        var signatureFactory = new Asn1SignatureFactory("SHA256WITHECDSA", keyPair.Private);
        return certificateGenerator.Generate(signatureFactory);
    }
}
