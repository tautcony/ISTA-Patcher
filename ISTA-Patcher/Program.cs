// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher;

using System.Security.Cryptography;
using System.Text;
using CommandLine;
using ConsoleTables;
using ISTA_Patcher.Core;
using ISTA_Patcher.Core.Patcher;
using ISTA_Patcher.Utils.LicenseManagement;
using ISTA_Patcher.Utils.LicenseManagement.CoreFramework;
using Serilog;
using Serilog.Core;
using DecryptOptions = ProgramArgs.DecryptOptions;
using LicenseOptions = ProgramArgs.LicenseOptions;
using PatchOptions = ProgramArgs.PatchOptions;

internal static class ISTAPatcher
{
    private static LoggingLevelSwitch LevelSwitch { get; } = new();

    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.ControlledBy(LevelSwitch)
                     .WriteTo.Console()
                     .CreateLogger();

        return await Parser.Default.ParseArguments<PatchOptions, DecryptOptions, LicenseOptions>(args)
                     .MapResult(
                         (PatchOptions opts) => RunPatchAndReturnExitCode(opts),
                         (DecryptOptions opts) => RunDecryptAndReturnExitCode(opts),
                         (LicenseOptions opts) => RunLicenseOperationAndReturnExitCode(opts),
                         _ => Task.FromResult(1));
    }

    private static Task<int> RunPatchAndReturnExitCode(PatchOptions opts)
    {
        LevelSwitch.MinimumLevel = opts.Verbosity;
        var guiBasePath = Utils.Constants.TesterGUIPath.Aggregate(opts.TargetPath, Path.Join);
        var psdzBasePath = Utils.Constants.PSdZPath.Aggregate(opts.TargetPath, Path.Join);

        if (!Directory.Exists(guiBasePath) || !Directory.Exists(psdzBasePath))
        {
            Log.Fatal("Folder structure does not match, please check options");
            return Task.FromResult(-1);
        }

        IPatcher patcher = opts.PatchType switch
        {
            ProgramArgs.PatchType.B => new DefaultPatcher(opts),
            ProgramArgs.PatchType.T => new ToyotaPatcher(),
            _ => throw new NotImplementedException(),
        };

        Patch.PatchISTA(patcher, opts);
        return Task.FromResult(0);
    }

    private static async Task<int> RunDecryptAndReturnExitCode(DecryptOptions opts)
    {
        LevelSwitch.MinimumLevel = opts.Verbosity;
        var encryptedFileList = Utils.Constants.EncCnePath.Aggregate(opts.TargetPath, Path.Join);
        var basePath = Path.Join(opts.TargetPath, Utils.Constants.TesterGUIPath[0]);
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

        var table = new ConsoleTable("FilePath", "Hash(SHA256)", "Integrity");

        foreach (var fileInfo in fileList)
        {
            if (opts.Integrity)
            {
                var checkResult = await CheckFileIntegrity(basePath, fileInfo);
                var info = string.IsNullOrEmpty(checkResult.Value) ? fileInfo.FilePath : $"{fileInfo.FilePath} ({checkResult.Value})";
                table.AddRow(info, fileInfo.Hash, checkResult.Key);
            }
            else
            {
                table.AddRow(fileInfo.FilePath, fileInfo.Hash, "/");
            }
        }

        Log.Information("Markdown result:{NewLine}{Markdown}", Environment.NewLine, table.ToMarkDownString());
        return 0;
    }

    private static async Task<KeyValuePair<string, string>> CheckFileIntegrity(string basePath, HashFileInfo fileInfo)
    {
        string checkResult;
        var version = string.Empty;
        var filePath = Path.Join(basePath, fileInfo.FilePath);
        if (!File.Exists(filePath))
        {
            return new KeyValuePair<string, string>("Not Found", string.Empty);
        }

        try
        {
            var module = Core.PatchUtils.LoadModule(filePath);
            version = module.Assembly.Version.ToString();
        }
        catch (System.BadImageFormatException)
        {
            Log.Warning("None .NET assembly found: {FilePath}", filePath);
        }

        if (fileInfo.Hash == string.Empty)
        {
            checkResult = "[EMPTY]";
        }
        else
        {
            var realHash = await HashFileInfo.CalculateHash(filePath);
            checkResult = realHash == fileInfo.Hash ? "[OK]" : "[NG]";
        }

        if (!OperatingSystem.IsWindows())
        {
            return new KeyValuePair<string, string>(checkResult, version);
        }

        var wasVerified = false;

        var bChecked = NativeMethods.StrongNameSignatureVerificationEx(filePath, true, ref wasVerified);
        if (bChecked)
        {
            checkResult += wasVerified ? "[S:OK]" : "[S:NG]";
        }
        else
        {
            checkResult += "[S:NF]";
        }

        return new KeyValuePair<string, string>(checkResult, version);
    }

    private static async Task<int> RunLicenseOperationAndReturnExitCode(LicenseOptions opts)
    {
        LevelSwitch.MinimumLevel = opts.Verbosity;

        if (!string.IsNullOrEmpty(opts.Decode))
        {
            try
            {
                var str = Encoding.UTF8.GetString(Convert.FromHexString(opts.Decode));
                Log.Information("Decoded string: {String}", str);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while decoding string");
            }

            return 0;
        }

        var privateKeyPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "private-key.xml");

        string keyPairXml = null;
        if (opts.KeyPairPath != null)
        {
            if (!File.Exists(opts.KeyPairPath))
            {
                Log.Error("Private key pair {KeyPairPath} does not exist", opts.KeyPairPath);
                return -1;
            }

            await using var fs = File.OpenRead(opts.KeyPairPath);
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            keyPairXml = await sr.ReadToEndAsync();
            Log.Debug("Loaded private key from {KeyPairPath}", opts.KeyPairPath);
        }

        string licenseXml = null;
        if (opts.LicenseRequestPath != null)
        {
            if (opts.Base64)
            {
                try
                {
                    var data = Convert.FromBase64String(opts.LicenseRequestPath);
                    licenseXml = Encoding.UTF8.GetString(data);
                    Log.Debug("Loaded license request from parameter");
                }
                catch (FormatException ex)
                {
                    Log.Error(ex, "License request is not a valid base64 string");
                    return -1;
                }
            }
            else
            {
                if (!File.Exists(opts.LicenseRequestPath))
                {
                    Log.Error("License request file {LicensePath} does not exist", opts.LicenseRequestPath);
                    return -1;
                }

                await using var fs = File.OpenRead(opts.LicenseRequestPath);
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                licenseXml = await sr.ReadToEndAsync();
                Log.Debug("Loaded license request from {LicensePath}", opts.LicenseRequestPath);
            }
        }

        // --generate
        if (opts.GenerateKeyPair)
        {
            await GenerateKeyPair(privateKeyPath, opts.dwKeySize);
            return 0;
        }

        // --sign
        if (opts.SignLicense && keyPairXml != null && licenseXml != null)
        {
            return await SignLicense(keyPairXml, licenseXml, opts);
        }

        // --patch
        if (keyPairXml != null && opts.TargetPath != null)
        {
            if (!Directory.Exists(opts.TargetPath))
            {
                Log.Error("Target directory {TargetPath} does not exist", opts.TargetPath);
                return -1;
            }

            // Patch program
            var rsaCryptoServiceProvider = new RSACryptoServiceProvider(opts.dwKeySize);
            rsaCryptoServiceProvider.FromXmlString(keyPairXml);

            var parameters = rsaCryptoServiceProvider.ExportParameters(true);

            var modulus = Convert.ToBase64String(parameters.Modulus);
            var exponent = Convert.ToBase64String(parameters.Exponent);

            Patch.PatchISTA(new DefaultLicensePatcher(modulus, exponent, opts), new PatchOptions
            {
                Restore = opts.Restore,
                Verbosity = opts.Verbosity,
                TargetPath = opts.TargetPath,
                Force = opts.Force,
                Deobfuscate = opts.Deobfuscate,
            });

            return 0;
        }

        Log.Warning("No operation matched, exiting...");
        return 1;
    }

    private static async Task GenerateKeyPair(string privateKeyPath, int dwKeySize)
    {
        using var rsa = new RSACryptoServiceProvider(dwKeySize);
        try
        {
            var privateKey = rsa.ToXmlString(true);

            await using var fs = new FileStream(privateKeyPath, FileMode.Create);
            await using var sw = new StreamWriter(fs);
            await sw.WriteAsync(privateKey);

            Log.Information("Generated key pair located at {PrivateKeyPath}", privateKeyPath);
        }
        finally
        {
            rsa.PersistKeyInCsp = false;
            rsa.Clear();
        }
    }

    private static async Task<int> SignLicense(string keyPairXml, string licenseXml, LicenseOptions opts)
    {
        var license = LicenseInfoSerializer.FromString<LicenseInfo>(licenseXml);
        if (license == null)
        {
            Log.Error("License request is not valid");
            return -1;
        }

        var isValid = false;
        if (license.LicenseKey is { Length: > 0 })
        {
            // verify license
            var deformatter = LicenseStatusChecker.GetRSAPKCS1SignatureDeformatter(keyPairXml);
            isValid = LicenseStatusChecker.IsLicenseValid(license, deformatter);
            Log.Information("License is valid: {IsValid}", isValid);
        }

        if (isValid)
        {
            Log.Debug("License is valid, no need to patch");
            return 0;
        }

        // update license info
        license.Comment = $"{Core.PatchUtils.PoweredBy} ({Core.PatchUtils.RepoUrl})";
        license.Expiration = DateTime.MaxValue;
        if (license.SubLicenses != null)
        {
            foreach (var subLicense in license.SubLicenses)
            {
                if (opts.SyntheticEnv)
                {
                    subLicense.PackageName = "SyntheticEnv";
                }

                subLicense.PackageRule ??= "true";
                subLicense.PackageExpire = DateTime.MaxValue;
            }
        }

        // generate license key
        LicenseStatusChecker.GenerateLicenseKey(license, keyPairXml);
        var signedLicense = LicenseInfoSerializer.ToByteArray(license);
        if (opts.SignedLicensePath != null)
        {
            await using var fileStream = File.Create(opts.SignedLicensePath);
            await fileStream.WriteAsync(signedLicense);
        }
        else
        {
            Log.Information("License[Base64]:{NewLine}{License}", Environment.NewLine, Convert.ToBase64String(signedLicense));
            Log.Information("License[Xml]:{NewLine}{License}", Environment.NewLine, LicenseInfoSerializer.ToString(license).ReplaceLineEndings(string.Empty));
        }

        return 0;
    }
}
