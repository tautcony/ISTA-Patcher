// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable PropertyCanBeMadeInitOnly.Global, NotNullOrRequiredMemberIsNotInitialized
namespace ISTA_Patcher;

using CommandLine;

public static class ProgramArgs
{
    public enum PatchType
    {
        B = 0,
        T = 1,
    }

    public class BaseOptions
    {
        [Option('v', "verbosity", Default = Serilog.Events.LogEventLevel.Information, HelpText = "Set the output verbosity level.")]
        public Serilog.Events.LogEventLevel Verbosity { get; set; }

        [Option('r', "restore", Default = false, HelpText = "Restore patched files")]
        public bool Restore { get; set; }
    }

    public class OptionalPatchOptions : BaseOptions
    {
        [Option("enable-enet", Default = false, HelpText = "Enable ENET programming.")]
        public bool EnableENET { get; set; }

        [Option("disable-requirements-check", Default = false, HelpText = "Disable system requirements check.")]
        public bool DisableRequirementsCheck { get; set; }

        [Option("enable-not-send", Default = false, HelpText = "Enable VIN Not Send Data")]
        public bool EnableNotSend { get; set; }
    }

    [Verb("patch", HelpText = "Perform patching on application and library.")]
    public class PatchOptions : OptionalPatchOptions
    {
        [Option('t', "type", Default = ProgramArgs.PatchType.B, HelpText = "Specify the patch type. Valid options: B, T.")]
        public PatchType PatchType { get; set; }

        [Option("skip-validation-patch", Default = false, HelpText = "Skip license validation patch.")]
        public bool SkipLicensePatch { get; set; }

        [Option("deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }

        [Option('f', "force", Default = false, HelpText = "Force patching on application and library.")]
        public bool Force { get; set; }

        [Value(1, MetaName = "ISTA-P path", Required = true, HelpText = "Specify the path for ISTA-P.")]
        public string TargetPath { get; set; }
    }

    [Verb("license", HelpText = "Perform license-related operations.")]
    public class LicenseOptions : OptionalPatchOptions
    {
        [Option('g', "generate", HelpText = "Generate a key pair.", Group = "operation")]
        public bool GenerateKeyPair { get; set; }

        [Option('p', "patch", HelpText = "Patch the target program.", Group = "operation")]
        public string? TargetPath { get; set; }

        [Option('s', "sign", HelpText = "Sign a license request.", Group = "operation")]
        public bool SignLicense { get; set; }

        [Option("decode", Default = null, HelpText = "Decode the license request stream.", Group = "operation")]
        public string Decode { get; set; }

        [Option("auto", Default = false, HelpText = "Automatically generate a key pair and patch the target program.")]
        public bool AutoMode { get; set; }

        [Option('k', "key-pair", HelpText = "Specify the path for the key pair file.")]
        public string? KeyPairPath { get; set; }

        [Option('l', "license", HelpText = "Specify the path for the license request file or provide base64-encoded content.")]
        public string? LicenseRequestPath { get; set; }

        [Option("synthetic", Default = false, HelpText = "Update all sub-licenses package name to SyntheticEnv.")]
        public bool SyntheticEnv { get; set; }

        [Option('o', "output", HelpText = "Specify the target path for the signed license file.")]
        public string? SignedLicensePath { get; set; }

        [Option('b', "base64", HelpText = "Treat the license request option as base64-encoded content.")]
        public bool Base64 { get; set; }

        [Option('f', "force", Default = false, HelpText = "Force patching on application and library.")]
        public bool Force { get; set; }

        [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt the integrity checklist.")]
    public class DecryptOptions : BaseOptions
    {
        [Option('i', "integrity", Default = false, HelpText = "Verify the integrity of the checklist.")]
        public bool Integrity { get; set; }

        [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Specify the path for ISTA-P.")]
        public string? TargetPath { get; set; }
    }
}
