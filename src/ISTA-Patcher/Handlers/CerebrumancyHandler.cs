// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Handlers;

using System.Security.Cryptography;
using System.Text;
using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Core.Patcher.Provider;
using ISTAlter.Models.Rheingold.LicenseManagement;
using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;
using ISTAlter.Utils;
using Serilog;

public static class CerebrumancyHandler
{
    public static async Task<int> Execute(ISTAOptions.CerebrumancyOptions opts)
    {
        using var transaction = new TransactionHandler("ISTA-Patcher", "cerebrumancy");
        opts.Transaction = transaction;
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;

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

        string? primamindXml = null;
        if (opts.LoadPrimamind != null)
        {
            if (!File.Exists(opts.LoadPrimamind))
            {
                Log.Error("Primamind {Primamind} does not exist", opts.LoadPrimamind);
                return -1;
            }

            await using var fs = File.OpenRead(opts.LoadPrimamind);
            using var sr = new StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            primamindXml = await sr.ReadToEndAsync().ConfigureAwait(false);
            Log.Debug("Loaded primamind from {Primamind}", opts.LoadPrimamind);
        }

        string? solicitationXml = null;
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

            using var rsaCryptoServiceProvider = new RSACryptoServiceProvider(opts.primamindIntensity);
            rsaCryptoServiceProvider.FromXmlString(primamindXml);

            var parameters = rsaCryptoServiceProvider.ExportParameters(includePrivateParameters: true);

            var modulus = Convert.ToBase64String(parameters.Modulus);
            var exponent = Convert.ToBase64String(parameters.Exponent);

            Patch.PatchISTA(new DefaultSolicitationPatcherProvider(modulus, exponent, opts), new ISTAOptions.PatchOptions
            {
                Restore = opts.Restore,
                Verbosity = opts.Verbosity,
                TargetPath = opts.Mentacorrosion,
                Force = opts.Compulsion,
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

            await using var fs = new FileStream(carvedPrimamindPath, FileMode.Create);
            await using var sw = new StreamWriter(fs);
            await sw.WriteAsync(privateKey).ConfigureAwait(false);
            Log.Information("Generated key pair located at {CarvedPrimamindPath}", carvedPrimamindPath);
        }
        finally
        {
            rsa.PersistKeyInCsp = false;
            rsa.Clear();
        }
    }

    private static async Task<int> ConcretizePrimamind(string primamindXml, string solicitationXml, ISTAOptions.CerebrumancyOptions opts)
    {
        var license = LicenseInfoSerializer.FromString<LicenseInfo>(solicitationXml);
        if (license == null)
        {
            Log.Error("Solicitation is not valid");
            return -1;
        }

        var isValid = false;
        if (license.LicenseKey is { Length: > 0 })
        {
            var deformatter = LicenseStatusChecker.GetRSAPKCS1SignatureDeformatter(primamindXml);
            isValid = LicenseStatusChecker.IsLicenseValid(license, deformatter);
            Log.Information("Solicitation is valid: {IsValid}", isValid);
        }

        if (isValid)
        {
            Log.Debug("Solicitation is valid, no need to concretize");
            return 0;
        }

        license.Comment = $"{Encoding.UTF8.GetString(PatchUtils.Config)} ({Encoding.UTF8.GetString(PatchUtils.Source)})";
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
            await using var fileStream = File.Create(opts.Manifestation);
            await fileStream.WriteAsync(signedLicense).ConfigureAwait(false);
        }
        else
        {
            Log.Information("Manifestation[Base64]:{NewLine}{Manifestation}", Environment.NewLine, Convert.ToBase64String(signedLicense));
            Log.Information("Manifestation[Xml]:{NewLine}{Manifestation}", Environment.NewLine, LicenseInfoSerializer.ToString(license).ReplaceLineEndings(string.Empty));
        }

        return 0;
    }
}
