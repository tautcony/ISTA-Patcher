// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

// ReSharper disable PropertyCanBeMadeInitOnly.Global, NotNullOrRequiredMemberIsNotInitialized
namespace ISTA_Patcher;

using System.CommandLine;

public static class ProgramArgs
{
    public enum PatchType
    {
        B = 0,
        T = 1,
    }

    public class BaseOptions
    {
        // [Option('v', "verbosity", Default = Serilog.Events.LogEventLevel.Information, HelpText = "Set the output verbosity level.")]
        public Serilog.Events.LogEventLevel Verbosity { get; set; }

        // [Option('r', "restore", Default = false, HelpText = "Restore patched files")]
        public bool Restore { get; set; }
    }

    public class OptionalPatchOptions : BaseOptions
    {
        // [Option("enable-enet", Default = false, HelpText = "Enable ENET programming.")]
        public bool EnableENET { get; set; }

        // [Option("disable-requirements-check", Default = false, HelpText = "Disable system requirements check.")]
        public bool DisableRequirementsCheck { get; set; }

        // [Option("enable-not-send", Default = false, HelpText = "Enable VIN Not Send Data.")]
        public bool EnableNotSend { get; set; }

        // [Option("skip-validation-patch", Default = false, HelpText = "Skip license validation patch.")]
        public bool SkipLicensePatch { get; set; }

        // [Option("patch-user-auth", Default = false, HelpText = "Patch user authentication environment.")]
        public bool UserAuthEnv { get; set; }
    }

    // [Verb("patch", HelpText = "Perform patching on application and library.")]
    public class PatchOptions : OptionalPatchOptions
    {
        // [Option('t', "type", Default = ProgramArgs.PatchType.B, HelpText = "Specify the patch type. Valid options: B, T.")]
        public PatchType PatchType { get; set; }

        // [Option("generate-reg-file", Default = false, HelpText = "Generate mock reg file.")]
        public bool GenerateMockRegFile { get; set; }

        // [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }

        // [Option('f', "force", Default = false, HelpText = "Force patching on application and library.")]
        public bool Force { get; set; }

        // [Value(1, MetaName = "ISTA-P path", Required = true, HelpText = "Specify the path for ISTA-P.")]
        public string TargetPath { get; set; }
    }

    // [Verb("license", HelpText = "Perform license-related operations.")]
    public class LicenseOptions : OptionalPatchOptions
    {
        // [Option('g', "generate", HelpText = "Generate a key pair.", Group = "operation")]
        public bool GenerateKeyPair { get; set; }

        // [Option("key-size", Default = 1024, HelpText = "The size of the key to use in bits")]
        public int dwKeySize { get; set; }

        // [Option('p', "patch", HelpText = "Patch the target program.", Group = "operation")]
        public string? TargetPath { get; set; }

        // [Option('s', "sign", HelpText = "Sign a license request.", Group = "operation")]
        public bool SignLicense { get; set; }

        // [Option("decode", Default = null, HelpText = "Decode the license request stream.", Group = "operation")]
        public string Decode { get; set; }

        // [Option('k', "key-pair", HelpText = "Specify the path for the key pair file.")]
        public string? KeyPairPath { get; set; }

        // [Option('l', "license", HelpText = "Specify the path for the license request file or provide base64-encoded content.")]
        public string? LicenseRequestPath { get; set; }

        // [Option("synthetic", Default = false, HelpText = "Update all sub-licenses package name to SyntheticEnv.")]
        public bool SyntheticEnv { get; set; }

        // [Option('o', "output", HelpText = "Specify the target path for the signed license file.")]
        public string? SignedLicensePath { get; set; }

        // [Option('b', "base64", HelpText = "Treat the license request option as base64-encoded content.")]
        public bool Base64 { get; set; }

        // [Option('f', "force", Default = false, HelpText = "Force patching on application and library.")]
        public bool Force { get; set; }

        // [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }
    }

    // [Verb("decrypt", HelpText = "Decrypt the integrity checklist.")]
    public class DecryptOptions : BaseOptions
    {
        // [Option('i', "integrity", Default = false, HelpText = "Verify the integrity of the checklist.")]
        public bool Integrity { get; set; }

        // [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Specify the path for ISTA-P.")]
        public string? TargetPath { get; set; }
    }

    // base options
    private static readonly CliOption<Serilog.Events.LogEventLevel> VerbosityOption = new("-v", "--verbosity")
    {
        DefaultValueFactory = _ => Serilog.Events.LogEventLevel.Information,
        Description = "Set the output verbosity level.",
    };

    private static readonly CliOption<bool> RestoreOption = new("-r", "--restore")
    {
        DefaultValueFactory = _ => false,
        Description = "Restore patched files",
    };

    // optional patch options
    private static readonly CliOption<bool> EnableEnetOption = new("--enable-enet")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable ENET programming.",
    };

    private static readonly CliOption<bool> DisableRequirementsCheckOption = new("--disable-requirements-check")
    {
        DefaultValueFactory = _ => false,
        Description = "Disable system requirements check.",
    };

    private static readonly CliOption<bool> EnableNotSendOption = new("--enable-not-send")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable VIN Not Send Data.",
    };

    private static readonly CliOption<bool> SkipValidationPatchOption = new("--skip-validation-patch")
    {
        DefaultValueFactory = _ => false,
        Description = "Skip license validation patch.",
    };

    private static readonly CliOption<bool> PatchUserAuthOption = new("--patch-user-auth")
    {
        DefaultValueFactory = _ => false,
        Description = "Patch user authentication environment.",
    };

    public static CliCommand buildPatchCommand(Func<PatchOptions, Task<int>> handler)
    {
        // patch options
        var typeOption = new CliOption<ProgramArgs.PatchType>("-t", "--type")
        {
            DefaultValueFactory = _ => ProgramArgs.PatchType.B,
            Description = "Specify the patch type. Valid options: B, T.",
        };
        var generateRegFileOption = new CliOption<bool>("--generate-reg-file")
        {
            DefaultValueFactory = _ => false,
            Description = "Generate mock reg file.",
        };
        var deobfuscateOption = new CliOption<bool>("-d", "--deobfuscate"
        )
        {
            DefaultValueFactory = _ => false,
            Description = "Deobfuscate application and library.",
        };
        var forceOption = new CliOption<bool>("-f", "--force")
        {
            DefaultValueFactory = _ => false,
            Description = "Force patching on application and library.",
        };
        var targetPathArgument = new CliArgument<string>("targetPath")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the path for ISTA-P.",
        };

        var patchCommand = new CliCommand("patch", "Perform patching on application and library.")
        {
            VerbosityOption,
            RestoreOption,
            EnableEnetOption,
            DisableRequirementsCheckOption,
            EnableNotSendOption,
            SkipValidationPatchOption,
            PatchUserAuthOption,
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

    public static CliCommand buildLicenseCommand(Func<LicenseOptions, Task<int>> handler)
    {
        // License options
        var generateKeyPairOption = new CliOption<bool>("-g", "--generate")
        {
            DefaultValueFactory = _ => false,
            Description = "Generate a key pair.",
        };
        var keySizeOption = new CliOption<int>("--key-size")
        {
            DefaultValueFactory = _ => 1024,
            Description = "The size of the key to use in bits",
        };
        var patchOption = new CliOption<string>("-p", "--patch")
        {
            DefaultValueFactory = _ => null,
            Description = "Patch the target program.",
        };
        var signLicenseOption = new CliOption<bool>("-s", "--sign")
        {
            DefaultValueFactory = _ => false,
            Description = "Sign a license request.",
        };
        var decodeOption = new CliOption<string>("--decode")
        {
            DefaultValueFactory = _ => null,
            Description = "Decode the license request stream.",
        };
        var keyPairPathOption = new CliOption<string>("-k", "--key-pair")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the path for the key pair file.",
        };
        var licenseRequestPathOption = new CliOption<string>("-l", "--license")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the path for the license request file or provide base64-encoded content.",
        };
        var syntheticEnvOption = new CliOption<bool>("--synthetic")
        {
            DefaultValueFactory = _ => false,
            Description = "Add a sub-license package with name SyntheticEnv.",
        };
        var signedLicensePathOption = new CliOption<string>("-o", "--output")
        {
            DefaultValueFactory = _ => null,
            Description = "Specify the target path for the signed license file.",
        };
        var base64Option = new CliOption<bool>("-b", "--base64")
        {
            DefaultValueFactory = _ => false,
            Description = "Treat the license request option as base64-encoded content.",
        };
        var forceOption = new CliOption<bool>("-f", "--force")
        {
            DefaultValueFactory = _ => false,
            Description = "Force patching on application and library.",
        };
        var deobfuscateOption = new CliOption<bool>("-d", "--deobfuscate")
        {
            DefaultValueFactory = _ => false,
            Description = "Deobfuscate application and library.",
        };

        var licenseCommand = new CliCommand("license", "Perform license-related operations.")
        {
            VerbosityOption,
            RestoreOption,
            EnableEnetOption,
            DisableRequirementsCheckOption,
            EnableNotSendOption,
            SkipValidationPatchOption,
            PatchUserAuthOption,
            generateKeyPairOption,
            keySizeOption,
            patchOption,
            signLicenseOption,
            decodeOption,
            keyPairPathOption,
            licenseRequestPathOption,
            syntheticEnvOption,
            signedLicensePathOption,
            base64Option,
            forceOption,
            deobfuscateOption,
        };

        licenseCommand.SetAction((result, _) =>
        {
            var verbosityValue = result.GetValue(VerbosityOption);
            var restoreValue = result.GetValue(RestoreOption);
            var enableEnetValue = result.GetValue(EnableEnetOption);
            var disableRequirementsCheckValue = result.GetValue(DisableRequirementsCheckOption);
            var enableNotSendValue = result.GetValue(EnableNotSendOption);
            var skipValidationPatchValue = result.GetValue(SkipValidationPatchOption);
            var patchUserAuthValue = result.GetValue(PatchUserAuthOption);
            var generateKeyPairValue = result.GetValue(generateKeyPairOption);
            var keySizeValue = result.GetValue(keySizeOption);
            var patchValue = result.GetValue(patchOption);
            var signLicenseValue = result.GetValue(signLicenseOption);
            var decodeValue = result.GetValue(decodeOption);
            var keyPairPathValue = result.GetValue(keyPairPathOption);
            var licenseRequestPathValue = result.GetValue(licenseRequestPathOption);
            var syntheticEnvValue = result.GetValue(syntheticEnvOption);
            var signedLicensePathValue = result.GetValue(signedLicensePathOption);
            var base64Value = result.GetValue(base64Option);
            var forceValue = result.GetValue(forceOption);
            var deobfuscateValue = result.GetValue(deobfuscateOption);

            var options = new LicenseOptions
            {
                Verbosity = verbosityValue,
                Restore = restoreValue,
                EnableENET = enableEnetValue,
                DisableRequirementsCheck = disableRequirementsCheckValue,
                EnableNotSend = enableNotSendValue,
                SkipLicensePatch = skipValidationPatchValue,
                UserAuthEnv = patchUserAuthValue,
                GenerateKeyPair = generateKeyPairValue,
                dwKeySize = keySizeValue,
                TargetPath = patchValue,
                SignLicense = signLicenseValue,
                Decode = decodeValue,
                KeyPairPath = keyPairPathValue,
                LicenseRequestPath = licenseRequestPathValue,
                SyntheticEnv = syntheticEnvValue,
                SignedLicensePath = signedLicensePathValue,
                Base64 = base64Value,
                Force = forceValue,
                Deobfuscate = deobfuscateValue,
            };
            return Task.FromResult(handler(options));
        });
        return licenseCommand;
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
        Func<LicenseOptions, Task<int>> licenseHandler,
        Func<DecryptOptions, Task<int>> decryptHandler
        )
    {
        var rootCommand = new CliRootCommand
        {
            Description = $"Copyright (C) 2022-2024 TautCony.\n" +
                          $"Released under the GNU GPLv3+.",
        };

        var patchCommand = buildPatchCommand(patchHandler);
        var licenseCommand = buildLicenseCommand(licenseHandler);
        var decryptCommand = buildDecryptCommand(decryptHandler);

        rootCommand.Add(patchCommand);
        rootCommand.Add(licenseCommand);
        rootCommand.Add(decryptCommand);

        return rootCommand;
    }
}
