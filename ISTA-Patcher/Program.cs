// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher;

using System.Security.Cryptography;
using System.Text;
using ConsoleTables;
using ISTA_Patcher.Core;
using ISTA_Patcher.Core.Patcher;
using ISTA_Patcher.Utils.LicenseManagement;
using ISTA_Patcher.Utils.LicenseManagement.CoreFramework;
using Sentry.Profiling;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using DecryptOptions = ProgramArgs.DecryptOptions;
using PatchOptions = ProgramArgs.PatchOptions;

internal static class Program
{
    private static LoggingLevelSwitch LevelSwitch { get; } = new();

    public static Task<int> Main(string[] args)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://55e58df747fc4d43912790aa894700ba@o955448.ingest.sentry.io/4504370799116288";
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.SendDefaultPii = true;
            options.EnableTracing = true;
            options.TracesSampleRate = 1;
            options.AddIntegration(new ProfilingIntegration());
#if DEBUG
            options.Environment = "debug";
#endif
        });
        Log.Logger = new LoggerConfiguration()
                     .Enrich.FromLogContext()
                     .MinimumLevel.ControlledBy(LevelSwitch)
                     .WriteTo.Console()
                     .WriteTo.Sentry(LogEventLevel.Error, LogEventLevel.Debug)
                     .CreateLogger();

        var command = ProgramArgs.BuildCommandLine(RunPatchAndReturnExitCode, RunCerebrumancyOperationAndReturnExitCode, RunDecryptAndReturnExitCode);

        return command.Parse(args).InvokeAsync();
    }

    private static Task<int> RunPatchAndReturnExitCode(PatchOptions opts)
    {
        LevelSwitch.MinimumLevel = opts.Verbosity;
        var guiBasePath = Utils.Constants.TesterGUIPath.Aggregate(opts.TargetPath, Path.Join);
        var psdzBasePath = Utils.Constants.PSdZPath.Aggregate(opts.TargetPath, Path.Join);

        if (!Directory.Exists(guiBasePath) || !Directory.Exists(psdzBasePath))
        {
            Log.Fatal("Folder structure does not match under: {TargetPath}, please check options", opts.TargetPath);
            return Task.FromResult(-1);
        }

        IPatcher patcher = opts.PatchType switch
        {
            ProgramArgs.PatchType.B => new DefaultPatcher(opts),
            ProgramArgs.PatchType.T => new ToyotaPatcher(),
            _ => throw new NotSupportedException(),
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
                var checkResult = await CheckFileIntegrity(basePath, fileInfo).ConfigureAwait(false);
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
            var realHash = await HashFileInfo.CalculateHash(filePath).ConfigureAwait(false);
            checkResult = string.Equals(realHash, fileInfo.Hash, StringComparison.Ordinal) ? "[OK]" : "[NG]";
        }

        if (!OperatingSystem.IsWindows())
        {
            return new KeyValuePair<string, string>(checkResult, version);
        }

        var wasVerified = false;

        var bChecked = NativeMethods.StrongNameSignatureVerificationEx(filePath, fForceVerification: true, ref wasVerified);
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

    private static async Task<int> RunCerebrumancyOperationAndReturnExitCode(ProgramArgs.CerebrumancyOptions opts)
    {
        LevelSwitch.MinimumLevel = opts.Verbosity;

        if (!string.IsNullOrEmpty(opts.Mentalysis))
        {
            try
            {
                var str = Encoding.UTF8.GetString(Convert.FromHexString(opts.Mentalysis));
                Log.Information("Mentalysed string: {String}", str);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while mentalysing string");
            }

            return 0;
        }

        var carvedPrimamindPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "carved-primamind.xml");

        string primamindXml = null;
        if (opts.LoadPrimamind != null)
        {
            if (!File.Exists(opts.LoadPrimamind))
            {
                Log.Error("Primamind {Primamind} does not exist", opts.LoadPrimamind);
                return -1;
            }

            var fs = File.OpenRead(opts.LoadPrimamind);
            await using (fs.ConfigureAwait(false))
            {
                using var sr = new StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                primamindXml = await sr.ReadToEndAsync().ConfigureAwait(false);
                Log.Debug("Loaded primamind from {Primamind}", opts.LoadPrimamind);
            }
        }

        string solicitationXml = null;
        if (opts.Solicitation != null)
        {
            if (opts.Base64)
            {
                try
                {
                    var data = Convert.FromBase64String(opts.Solicitation);
                    solicitationXml = Encoding.UTF8.GetString(data);
                    Log.Debug("Loaded solicitation from parameter");
                }
                catch (FormatException ex)
                {
                    Log.Error(ex, "Solicitation is not a valid base64 string");
                    return -1;
                }
            }
            else
            {
                if (!File.Exists(opts.Solicitation))
                {
                    Log.Error("Solicitation {Solicitation} does not exist", opts.Solicitation);
                    return -1;
                }

                var fs = File.OpenRead(opts.Solicitation);
                await using (fs.ConfigureAwait(false))
                {
                    using var sr = new StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    solicitationXml = await sr.ReadToEndAsync().ConfigureAwait(false);
                    Log.Debug("Loaded solicitation from {LicensePath}", opts.Solicitation);
                }
            }
        }

        if (opts.CarvingPrimamind)
        {
            await CarvingPrimamind(carvedPrimamindPath, opts.primamindIntensity).ConfigureAwait(false);
            return 0;
        }

        if (opts.ConcretizePrimamind && primamindXml != null && solicitationXml != null)
        {
            return await ConcretizePrimamind(primamindXml, solicitationXml, opts).ConfigureAwait(false);
        }

        if (primamindXml != null && opts.Mentacorrosion != null)
        {
            if (!Directory.Exists(opts.Mentacorrosion))
            {
                Log.Error("Target directory {Mentacorrosion} does not exist", opts.Mentacorrosion);
                return -1;
            }

            var rsaCryptoServiceProvider = new RSACryptoServiceProvider(opts.primamindIntensity);
            rsaCryptoServiceProvider.FromXmlString(primamindXml);

            var parameters = rsaCryptoServiceProvider.ExportParameters(includePrivateParameters: true);

            var modulus = Convert.ToBase64String(parameters.Modulus);
            var exponent = Convert.ToBase64String(parameters.Exponent);

            Patch.PatchISTA(new DefaultLicensePatcher(modulus, exponent, opts), new PatchOptions
            {
                Restore = opts.Restore,
                Verbosity = opts.Verbosity,
                TargetPath = opts.Mentacorrosion,
                Force = opts.Compulsion,
                Deobfuscate = opts.SpecialisRevelio,
            });

            return 0;
        }

        Log.Warning("No operation matched, exiting...");
        return 1;
    }

    private static async Task CarvingPrimamind(string carvedPrimamindPath, int primamindIntensity)
    {
        using var rsa = new RSACryptoServiceProvider(primamindIntensity);
        try
        {
            var privateKey = rsa.ToXmlString(includePrivateParameters: true);

            var fs = new FileStream(carvedPrimamindPath, FileMode.Create);
            await using (fs.ConfigureAwait(false))
            {
                var sw = new StreamWriter(fs);
                await using (sw.ConfigureAwait(false))
                {
                    await sw.WriteAsync(privateKey).ConfigureAwait(false);

                    Log.Information("Generated key pair located at {CarvedPrimamindPath}", carvedPrimamindPath);
                }
            }
        }
        finally
        {
            rsa.PersistKeyInCsp = false;
            rsa.Clear();
        }
    }

    private static async Task<int> ConcretizePrimamind(string primamindXml, string solicitationXml, ProgramArgs.CerebrumancyOptions opts)
    {
        var license = LicenseInfoSerializer.FromString<LicenseInfo>(solicitationXml);
        if (license == null)
        {
            Log.Error("License request is not valid");
            return -1;
        }

        var isValid = false;
        if (license.LicenseKey is { Length: > 0 })
        {
            var deformatter = LicenseStatusChecker.GetRSAPKCS1SignatureDeformatter(primamindXml);
            isValid = LicenseStatusChecker.IsLicenseValid(license, deformatter);
            Log.Information("License is valid: {IsValid}", isValid);
        }

        if (isValid)
        {
            Log.Debug("Solicitation is valid, no need to concretize");
            return 0;
        }

        license.Comment = $"{Core.PatchUtils.Config} ({Encoding.UTF8.GetString(Core.PatchUtils.Source)})";
        license.Expiration = DateTime.MaxValue;
        if (license.SubLicenses != null)
        {
            if (opts.SyntheticEnv)
            {
                license.SubLicenses.Add(new LicensePackage
                {
                    PackageName = "SyntheticEnv",
                });
            }

            foreach (var subLicense in license.SubLicenses)
            {
                subLicense.PackageRule ??= "true";
                subLicense.PackageExpire = DateTime.MaxValue;
            }
        }

        LicenseStatusChecker.GenerateLicenseKey(license, primamindXml);
        var signedLicense = LicenseInfoSerializer.ToByteArray(license);
        if (opts.Manifestation != null)
        {
            var fileStream = File.Create(opts.Manifestation);
            await using (fileStream.ConfigureAwait(false))
            {
                await fileStream.WriteAsync(signedLicense).ConfigureAwait(false);
            }
        }
        else
        {
            Log.Information("Manifestation[Base64]:{NewLine}{Manifestation}", Environment.NewLine, Convert.ToBase64String(signedLicense));
            Log.Information("Manifestation[Xml]:{NewLine}{Manifestation}", Environment.NewLine, LicenseInfoSerializer.ToString(license).ReplaceLineEndings(string.Empty));
        }

        return 0;
    }
}
