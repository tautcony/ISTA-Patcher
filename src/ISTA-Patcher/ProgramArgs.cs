// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

namespace ISTAPatcher;

using System.CommandLine;
using System.Text;
using ISTAlter;
using ISTAlter.Core;

public static class ProgramArgs
{
    // base options
    private static readonly CliOption<Serilog.Events.LogEventLevel> VerbosityOption = new("-v", "--verbosity")
    {
        DefaultValueFactory = _ => Serilog.Events.LogEventLevel.Information,
        Description = "Specify the verbosity level of the output.",
    };

    private static readonly CliOption<bool> RestoreOption = new("-r", "--restore")
    {
        DefaultValueFactory = _ => false,
        Description = "Restore the patched files to their original state.",
    };

    // optional patch options
    private static readonly CliOption<bool> EnableEnetOption = new("--enable-enet")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable ENET programming functionality.",
    };

    private static readonly CliOption<bool> EnableFinishedOperationsOption = new("--enable-finished-op")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable to open finished operations functionality.",
    };

    private static readonly CliOption<bool> EnableSkipRequirementsCheckOption = new("--enable-skip-system-check")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable skip the system requirements check functionality.",
    };

    private static readonly CliOption<bool> EnableDataNotSendOption = new("--enable-data-not-send")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable VIN Not Send Data functionality.",
    };

    private static readonly CliOption<bool> PatchUserAuthOption = new("--patch-user-auth")
    {
        DefaultValueFactory = _ => false,
        Description = "Patch the user authentication environment.",
    };

    private static readonly CliOption<bool> EnableSkipSyncClientConfigOption = new("--enable-skip-sync-client-config")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable skip sync client configuration functionality.",
    };

    private static readonly CliOption<bool> EnableSkipFakeFSCRejectOption = new("--enable-skip-fake-fsc-reject")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable skip fake FSC reject functionality.",
    };

    private static readonly CliOption<bool> EnableSkipAirClientOption = new("--enable-air-client")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable AIR Client functionality.",
    };

    private static readonly CliOption<ISTAOptions.ModeType> ModeOption = new("--mode")
    {
        DefaultValueFactory = _ => ISTAOptions.ModeType.Standalone,
        Description = "Specify the mode type.",
    };

    private static readonly CliOption<string> MarketLanguageOption = new("--market-language")
    {
        DefaultValueFactory = _ => null,
        Description = "Specify the market language.",
    };

    private static readonly CliOption<int> MaxDegreeOfParallelismOption = new("--max-degree-of-parallelism")
    {
        DefaultValueFactory = _ => Environment.ProcessorCount,
        Description = "Specify the maximum degree of parallelism for patching.",
    };

    public static CliCommand buildPatchCommand(Func<ISTAOptions.PatchOptions, Task<int>> handler)
    {
        // patch options
        var typeOption = new CliOption<ISTAOptions.PatchType>("-t", "--type")
        {
            DefaultValueFactory = _ => ISTAOptions.PatchType.B,
            Description = "Specify the patch type.",
        };
        var generateRegFileOption = new CliOption<bool>("--generate-registry-file")
        {
            DefaultValueFactory = _ => false,
            Description = "Generate a registry file.",
        };
        var forceOption = new CliOption<bool>("-f", "--force")
        {
            DefaultValueFactory = _ => false,
            Description = "Force patching on application and libraries.",
        };
        var skipLibraryOption = new CliOption<string[]>("--skip-library")
        {
            DefaultValueFactory = _ => [],
            Description = "Specify the libraries to skip patching.",
        };
        var targetPathArgument = new CliArgument<string>("targetPath")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the path for ISTA-P.",
        };

        var patchCommand = new CliCommand("patch", "Perform patching on application and libraries.")
        {
            typeOption,
            VerbosityOption,
            RestoreOption,
            forceOption,
            ModeOption,
            EnableEnetOption,
            EnableFinishedOperationsOption,
            EnableSkipRequirementsCheckOption,
            EnableDataNotSendOption,
            PatchUserAuthOption,
            MarketLanguageOption,
            EnableSkipSyncClientConfigOption,
            EnableSkipFakeFSCRejectOption,
            EnableSkipAirClientOption,
            MaxDegreeOfParallelismOption,
            generateRegFileOption,
            skipLibraryOption,
            targetPathArgument,
        };

        patchCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var restoreValue = result.GetValue(RestoreOption);
            var enableEnetValue = result.GetValue(EnableEnetOption);
            var enableFinishedOpValue = result.GetValue(EnableFinishedOperationsOption);
            var enableSkipRequirementsCheckValue = result.GetValue(EnableSkipRequirementsCheckOption);
            var enableDataNotSendValue = result.GetValue(EnableDataNotSendOption);
            var enableSyncClientConfigValue = result.GetValue(EnableSkipSyncClientConfigOption);
            var enableSkipFakeFSCRejectValue = result.GetValue(EnableSkipFakeFSCRejectOption);
            var enableAirClientValue = result.GetValue(EnableSkipAirClientOption);
            var typeValue = result.GetValue(typeOption);
            var modeValue = result.GetValue(ModeOption);
            var patchUserAuthValue = result.GetValue(PatchUserAuthOption);
            var marketLanguageValue = result.GetValue(MarketLanguageOption);
            var maxDegreeOfParallelismValue = result.GetValue(MaxDegreeOfParallelismOption);
            var generateRegFileValue = result.GetValue(generateRegFileOption);
            var forceValue = result.GetValue(forceOption);
            var skipLibraryValue = result.GetValue(skipLibraryOption);
            var targetPathValue = result.GetValue(targetPathArgument);

            var options = new ISTAOptions.PatchOptions
            {
                Verbosity = verbosityValue,
                Restore = restoreValue,
                ENET = enableEnetValue,
                FinishedOperations = enableFinishedOpValue,
                SkipRequirementsCheck = enableSkipRequirementsCheckValue,
                DataNotSend = enableDataNotSendValue,
                SkipSyncClientConfig = enableSyncClientConfigValue,
                SkipFakeFSCReject = enableSkipFakeFSCRejectValue,
                EnableAirClient = enableAirClientValue,
                Mode = modeValue,
                UserAuthEnv = patchUserAuthValue,
                MarketLanguage = marketLanguageValue,
                PatchType = typeValue,
                MaxDegreeOfParallelism = maxDegreeOfParallelismValue,
                GenerateMockRegFile = generateRegFileValue,
                Force = forceValue,
                SkipLibrary = skipLibraryValue,
                TargetPath = targetPathValue,
            };
            return Task.FromResult(handler(options));
        });
        return patchCommand;
    }

    public static CliCommand buildCerebrumancyCommand(Func<ISTAOptions.CerebrumancyOptions, Task<int>> handler)
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

        var cerebrumancyCommand = new CliCommand("cerebrumancy", "Initiate operations related to the manipulation of consciousness.")
        {
            VerbosityOption,
            RestoreOption,
            EnableEnetOption,
            EnableFinishedOperationsOption,
            EnableSkipRequirementsCheckOption,
            EnableDataNotSendOption,
            ModeOption,
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
        };

        cerebrumancyCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var restoreValue = result.GetValue(RestoreOption);
            var enetValue = result.GetValue(EnableEnetOption);
            var finishedOperationsValue = result.GetValue(EnableFinishedOperationsOption);
            var skipRequirementsCheckValue = result.GetValue(EnableSkipRequirementsCheckOption);
            var dataNotSendValue = result.GetValue(EnableDataNotSendOption);
            var modeOptionValue = result.GetValue(ModeOption);
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

            var options = new ISTAOptions.CerebrumancyOptions
            {
                Verbosity = verbosityValue,
                Restore = restoreValue,
                ENET = enetValue,
                FinishedOperations = finishedOperationsValue,
                SkipRequirementsCheck = skipRequirementsCheckValue,
                DataNotSend = dataNotSendValue,
                Mode = modeOptionValue,
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
            };
            return Task.FromResult(handler(options));
        });
        return cerebrumancyCommand;
    }

    public static CliCommand buildDecryptCommand(Func<ISTAOptions.DecryptOptions, Task<int>> handler)
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

            var options = new ISTAOptions.DecryptOptions
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

    public static CliCommand buildILeanCommand(Func<ISTAOptions.ILeanOptions, Task<int>> handler)
    {
        var machineGuidOption = new CliOption<string>("--machine-guid")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the machine GUID.",
        };

        var volumeSerialNumberOption = new CliOption<string>("--volume-serial-number")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the volume serial number.",
        };

        var showMachineInfoOption = new CliOption<bool>("--show-machine-info")
        {
            DefaultValueFactory = _ => false,
            Description = "Show the machine information.",
        };

        var encryptOption = new CliOption<string>("--encrypt")
        {
            DefaultValueFactory = _ => null,
            Description = "Encrypt the provided file.",
        };

        var decryptOption = new CliOption<string>("--decrypt")
        {
            DefaultValueFactory = _ => null,
            Description = "Decrypt the provided file.",
        };

        var iLeanCommand = new CliCommand("ilean", "Perform operations related to iLean.")
        {
            VerbosityOption,
            machineGuidOption,
            volumeSerialNumberOption,
            showMachineInfoOption,
            encryptOption,
            decryptOption,
        };

        iLeanCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var machineGuidValue = result.GetValue(machineGuidOption);
            var volumeSerialNumberValue = result.GetValue(volumeSerialNumberOption);
            var showMachineInfoValue = result.GetValue(showMachineInfoOption);
            var encryptValue = result.GetValue(encryptOption);
            var decryptValue = result.GetValue(decryptOption);

            var options = new ISTAOptions.ILeanOptions
            {
                Verbosity = verbosityValue,
                MachineGuid = machineGuidValue,
                VolumeSerialNumber = volumeSerialNumberValue,
                ShowMachineInfo = showMachineInfoValue,
                Encrypt = encryptValue,
                Decrypt = decryptValue,
            };
            return handler(options);
        });
        return iLeanCommand;
    }

    public static CliRootCommand BuildCommandLine(
        Func<ISTAOptions.PatchOptions, Task<int>> patchHandler,
        Func<ISTAOptions.CerebrumancyOptions, Task<int>> licenseHandler,
        Func<ISTAOptions.DecryptOptions, Task<int>> decryptHandler,
        Func<ISTAOptions.ILeanOptions, Task<int>> iLeanHandler
        )
    {
        var rootCommand = new CliRootCommand
        {
            Description = "Copyright (C) 2022-2025 TautCony.\n" +
                          $"Repo: {Encoding.UTF8.GetString(PatchUtils.Source)}\n" +
                          "Released under the GNU GPLv3+.",
        };

        var patchCommand = buildPatchCommand(patchHandler);
        var cerebrumancyCommand = buildCerebrumancyCommand(licenseHandler);
        var decryptCommand = buildDecryptCommand(decryptHandler);
        var iLeanCommand = buildILeanCommand(iLeanHandler);

        if (PatchUtils.Source.Length != PatchUtils.GetSourceCoefficients()[4][3] || PatchUtils.Config.Length == PatchUtils.GetCoefficients()[2][3])
        {
            return rootCommand;
        }

        rootCommand.Add(patchCommand);
        rootCommand.Add(cerebrumancyCommand);
        rootCommand.Add(decryptCommand);
        rootCommand.Add(iLeanCommand);

        return rootCommand;
    }
}
