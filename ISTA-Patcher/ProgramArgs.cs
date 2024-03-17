// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

// ReSharper disable PropertyCanBeMadeInitOnly.Global, NotNullOrRequiredMemberIsNotInitialized
namespace ISTA_Patcher;

using System.CommandLine;
using System.Text;
using ISTA_Patcher.Core;

public static class ProgramArgs
{
    public enum PatchType
    {
        B = 0,
        T = 1,
    }

    public class BaseOptions
    {
        public Serilog.Events.LogEventLevel Verbosity { get; set; }

        public bool Restore { get; set; }
    }

    public class OptionalPatchOptions : BaseOptions
    {
        public bool EnableENET { get; set; }

        public bool DisableRequirementsCheck { get; set; }

        public bool EnableNotSend { get; set; }

        public bool SkipLicensePatch { get; set; }

        public bool UserAuthEnv { get; set; }

        public bool DisableLogEnviroment { get; set; }

        public string? MarketLanguage { get; set; }

        public bool SkipSyncClientConfig { get; set; }
    }

    public class PatchOptions : OptionalPatchOptions
    {
        public PatchType PatchType { get; set; }

        public bool GenerateMockRegFile { get; set; }

        public bool Deobfuscate { get; set; }

        public bool Force { get; set; }

        public string TargetPath { get; set; }
    }

    public class CerebrumancyOptions : OptionalPatchOptions
    {
        public bool CarvingPrimamind { get; set; }

        public int primamindIntensity { get; set; }

        public string? Mentacorrosion { get; set; }

        public bool ConcretizePrimamind { get; set; }

        public string Mentalysis { get; set; }

        public string? LoadPrimamind { get; set; }

        public string? Solicitation { get; set; }

        public bool SyntheticEnv { get; set; }

        public string? Manifestation { get; set; }

        public bool Base64 { get; set; }

        public bool Compulsion { get; set; }

        public bool SpecialisRevelio { get; set; }
    }

    public class DecryptOptions : BaseOptions
    {
        public bool Integrity { get; set; }

        public string? TargetPath { get; set; }
    }

    // base options
    private static readonly CliOption<Serilog.Events.LogEventLevel> VerbosityOption = new("-v", "--verbosity")
    {
        DefaultValueFactory = _ => Serilog.Events.LogEventLevel.Information,
        Description = "[Element] Set the output verbosity level of the ISTA-Patcher's output.",
    };

    private static readonly CliOption<bool> RestoreOption = new("-r", "--restore")
    {
        DefaultValueFactory = _ => false,
        Description = "[General] Restore patched files to their original state.",
    };

    // optional patch options
    private static readonly CliOption<bool> EnableEnetOption = new("--enable-enet")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Enable ENET programming functionality.",
    };

    private static readonly CliOption<bool> DisableRequirementsCheckOption = new("--disable-requirements-check")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Disable system requirements check functionality.",
    };

    private static readonly CliOption<bool> EnableNotSendOption = new("--enable-not-send")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Enable VIN Not Send Data functionality.",
    };

    private static readonly CliOption<bool> SkipValidationPatchOption = new("--skip-validation-patch")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Skip license validation patch.",
    };

    private static readonly CliOption<bool> PatchUserAuthOption = new("--patch-user-auth")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Patch user authentication environment.",
    };

    private static readonly CliOption<bool> DisableLogEnviromentOption = new("--disable-log-enviroment")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Disable log environment object which may failed.",
    };

    private static readonly CliOption<string> MarketLanguageOption = new("--market-language")
    {
        DefaultValueFactory = _ => null,
        Description = "[Adjunct] Set the market language.",
    };

    private static readonly CliOption<bool> SkipSyncClientConfig = new("--skip-sync-client-config")
    {
        DefaultValueFactory = _ => false,
        Description = "[Adjunct] Skip sync client configuration.",
    };

    public static CliCommand buildPatchCommand(Func<PatchOptions, Task<int>> handler)
    {
        // patch options
        var typeOption = new CliOption<ProgramArgs.PatchType>("-t", "--type")
        {
            DefaultValueFactory = _ => ProgramArgs.PatchType.B,
            Description = "Specify the patch type. Valid options: B, T.",
        };
        var generateRegFileOption = new CliOption<bool>("--generate-registry-file")
        {
            DefaultValueFactory = _ => false,
            Description = "Generate a registry file.",
        };
        var deobfuscateOption = new CliOption<bool>("-d", "--deobfuscate")
        {
            DefaultValueFactory = _ => false,
            Description = "Enable deobfuscation of the application and libraries.",
        };
        var forceOption = new CliOption<bool>("-f", "--force")
        {
            DefaultValueFactory = _ => false,
            Description = "Force patching on application and libraries. If specified, ISTA-Patcher will apply patches forcefully, bypassing certain checks.",
        };
        var targetPathArgument = new CliArgument<string>("targetPath")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the path for ISTA-P. Provide the full path where ISTA-P is located on your system.",
        };

        var patchCommand = new CliCommand("patch", "Perform patching on application and libraries.")
        {
            VerbosityOption,
            RestoreOption,
            EnableEnetOption,
            DisableRequirementsCheckOption,
            EnableNotSendOption,
            SkipValidationPatchOption,
            PatchUserAuthOption,
            DisableLogEnviromentOption,
            MarketLanguageOption,
            SkipSyncClientConfig,
            typeOption,
            generateRegFileOption,
            deobfuscateOption,
            forceOption,
            targetPathArgument,
        };

        patchCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var restoreValue = result.GetValue(RestoreOption);
            var enableEnetValue = result.GetValue(EnableEnetOption);
            var disableRequirementsCheckValue = result.GetValue(DisableRequirementsCheckOption);
            var enableNotSendValue = result.GetValue(EnableNotSendOption);
            var skipValidationPatchValue = result.GetValue(SkipValidationPatchOption);
            var patchUserAuthValue = result.GetValue(PatchUserAuthOption);
            var disableLogEnviromentValue = result.GetValue(DisableLogEnviromentOption);
            var marketLanguageValue = result.GetValue(MarketLanguageOption);
            var skipSyncClientConfigValue = result.GetValue(SkipSyncClientConfig);
            var typeValue = result.GetValue(typeOption);
            var generateRegFileValue = result.GetValue(generateRegFileOption);
            var deobfuscateValue = result.GetValue(deobfuscateOption);
            var forceValue = result.GetValue(forceOption);
            var targetPathValue = result.GetValue(targetPathArgument);

            var options = new PatchOptions
            {
                Verbosity = verbosityValue,
                Restore = restoreValue,
                EnableENET = enableEnetValue,
                DisableRequirementsCheck = disableRequirementsCheckValue,
                EnableNotSend = enableNotSendValue,
                SkipLicensePatch = skipValidationPatchValue,
                UserAuthEnv = patchUserAuthValue,
                DisableLogEnviroment = disableLogEnviromentValue,
                MarketLanguage = marketLanguageValue,
                SkipSyncClientConfig = skipSyncClientConfigValue,
                PatchType = typeValue,
                GenerateMockRegFile = generateRegFileValue,
                Deobfuscate = deobfuscateValue,
                Force = forceValue,
                TargetPath = targetPathValue,
            };
            return Task.FromResult(handler(options));
        });
        return patchCommand;
    }

    public static CliCommand buildCerebrumancyCommand(Func<CerebrumancyOptions, Task<int>> handler)
    {
        var carvingPrimamindOption = new CliOption<bool>("--carving-primamind")
        {
            DefaultValueFactory = _ => false,
            Description = "Initiate the crafting ritual to sculpt a Primamind.",
        };
        var primamindIntensityOption = new CliOption<int>("--primamind-intensity")
        {
            DefaultValueFactory = _ => 1024,
            Description = "The arcane potency of the carved Primamind, measured in bits.",
        };
        var mentacorrosionOption = new CliOption<string>("--mentacorrosion")
        {
            DefaultValueFactory = _ => null,
            Description = "Invoke mentacorrosion upon the chosen target.",
        };
        var concretizePrimamindOption = new CliOption<bool>("--concretize-primamind")
        {
            DefaultValueFactory = _ => false,
            Description = "Initiate the ritual to materialize a Primamind entity.",
        };
        var mentalysisOption = new CliOption<string>("--mentalysis")
        {
            DefaultValueFactory = _ => null,
            Description = "Conduct mentalysing on the mystical stream.",
        };
        var loadPrimamindOption = new CliOption<string>("--load-primamind")
        {
            DefaultValueFactory = _ => null,
            Description = "Channel the arcane essence to summon and infuse the Primamind. Specify the conduit path.",
        };
        var solicitationOption = new CliOption<string>("--solicitation")
        {
            DefaultValueFactory = _ => null,
            Description = "Designate the path for the solicitation, or supply base64-encoded mystical essence.",
        };
        var syntheticEnvOption = new CliOption<bool>("--synthetic")
        {
            DefaultValueFactory = _ => false,
            Description = "Infuse the creation with an arcane essence, naming it SyntheticEnv.",
        };
        var manifestationOption = new CliOption<string>("--manifestation")
        {
            DefaultValueFactory = _ => null,
            Description = "Designate the destination for the manifestation.",
        };
        var base64Option = new CliOption<bool>("--base64")
        {
            DefaultValueFactory = _ => false,
            Description = "Interpret the solicitation request as base64-encoded mystical content.",
        };
        var compulsionOption = new CliOption<bool>("--compulsion")
        {
            DefaultValueFactory = _ => false,
            Description = "Invoke mystical forces to compel mentacorrosion.",
        };
        var specialisRevelioOption = new CliOption<bool>("--specialis-revelio")
        {
            DefaultValueFactory = _ => false,
            Description = "Initiate the spell 'Specialis Revelio' to unveil mysteries.",
        };

        var cerebrumancyCommand = new CliCommand("cerebrumancy", "Initiate operations related to the manipulation of consciousness.")
        {
            VerbosityOption,
            RestoreOption,
            EnableEnetOption,
            DisableRequirementsCheckOption,
            EnableNotSendOption,
            SkipValidationPatchOption,
            PatchUserAuthOption,
            carvingPrimamindOption,
            primamindIntensityOption,
            mentacorrosionOption,
            concretizePrimamindOption,
            mentalysisOption,
            loadPrimamindOption,
            solicitationOption,
            syntheticEnvOption,
            manifestationOption,
            base64Option,
            compulsionOption,
            specialisRevelioOption,
        };

        cerebrumancyCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var restoreValue = result.GetValue(RestoreOption);
            var enableEnetValue = result.GetValue(EnableEnetOption);
            var disableRequirementsCheckValue = result.GetValue(DisableRequirementsCheckOption);
            var enableNotSendValue = result.GetValue(EnableNotSendOption);
            var skipValidationPatchValue = result.GetValue(SkipValidationPatchOption);
            var patchUserAuthValue = result.GetValue(PatchUserAuthOption);
            var carvingPrimamindValue = result.GetValue(carvingPrimamindOption);
            var primamindIntensityValue = result.GetValue(primamindIntensityOption);
            var mentacorrosionValue = result.GetValue(mentacorrosionOption);
            var concretizePrimamindValue = result.GetValue(concretizePrimamindOption);
            var mentalysisValue = result.GetValue(mentalysisOption);
            var loadPrimamindValue = result.GetValue(loadPrimamindOption);
            var solicitationValue = result.GetValue(solicitationOption);
            var syntheticEnvValue = result.GetValue(syntheticEnvOption);
            var manifestationValue = result.GetValue(manifestationOption);
            var base64Value = result.GetValue(base64Option);
            var compulsionValue = result.GetValue(compulsionOption);
            var specialisRevelioValue = result.GetValue(specialisRevelioOption);

            var options = new CerebrumancyOptions
            {
                Verbosity = verbosityValue,
                Restore = restoreValue,
                EnableENET = enableEnetValue,
                DisableRequirementsCheck = disableRequirementsCheckValue,
                EnableNotSend = enableNotSendValue,
                SkipLicensePatch = skipValidationPatchValue,
                UserAuthEnv = patchUserAuthValue,
                CarvingPrimamind = carvingPrimamindValue,
                primamindIntensity = primamindIntensityValue,
                Mentacorrosion = mentacorrosionValue,
                ConcretizePrimamind = concretizePrimamindValue,
                Mentalysis = mentalysisValue,
                LoadPrimamind = loadPrimamindValue,
                Solicitation = solicitationValue,
                SyntheticEnv = syntheticEnvValue,
                Manifestation = manifestationValue,
                Base64 = base64Value,
                Compulsion = compulsionValue,
                SpecialisRevelio = specialisRevelioValue,
            };
            return Task.FromResult(handler(options));
        });
        return cerebrumancyCommand;
    }

    public static CliCommand buildDecryptCommand(Func<DecryptOptions, Task<int>> handler)
    {
        // Decrypt options
        var integrityOption = new CliOption<bool>("-i", "--integrity")
        {
            DefaultValueFactory = _ => false,
            Description = "Verify the integrity of the checklist.",
        };
        var targetPathArgument = new CliArgument<string>("targetPath")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the path for ISTA-P.",
        };

        var decryptCommand = new CliCommand("decrypt", "Decrypt the integrity checklist.")
        {
            VerbosityOption,
            RestoreOption,
            integrityOption,
            targetPathArgument,
        };

        decryptCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var restoreValue = result.GetValue(RestoreOption);
            var integrityValue = result.GetValue(integrityOption);
            var targetPathValue = result.GetValue(targetPathArgument);

            var options = new DecryptOptions
            {
                Verbosity = verbosityValue,
                Restore = restoreValue,
                Integrity = integrityValue,
                TargetPath = targetPathValue,
            };
            return handler(options);
        });
        return decryptCommand;
    }

    public static CliRootCommand BuildCommandLine(
        Func<PatchOptions, Task<int>> patchHandler,
        Func<CerebrumancyOptions, Task<int>> licenseHandler,
        Func<DecryptOptions, Task<int>> decryptHandler
        )
    {
        var rootCommand = new CliRootCommand
        {
            Description = "Copyright (C) 2022-2024 TautCony.\n" +
                          $"Repo: {Encoding.UTF8.GetString(PatchUtils.Source)}\n" +
                          "Released under the GNU GPLv3+.",
        };

        var patchCommand = buildPatchCommand(patchHandler);
        var licenseCommand = buildCerebrumancyCommand(licenseHandler);
        var decryptCommand = buildDecryptCommand(decryptHandler);

        if (PatchUtils.Source.Length != 40 || string.IsNullOrEmpty(PatchUtils.Config))
        {
            return rootCommand;
        }

        rootCommand.Add(patchCommand);
        rootCommand.Add(licenseCommand);
        rootCommand.Add(decryptCommand);

        return rootCommand;
    }
}
