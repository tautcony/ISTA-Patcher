// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher;

using CommandLine;

public static class ProgramArgs
{
    public enum PatchTypeEnum
    {
        BMW = 0,
        TOYOTA = 1,
    }

    public class BaseOption
    {
        [Option('v', "verbosity", Default = Serilog.Events.LogEventLevel.Information, HelpText = "Set output verbosity.")]
        public Serilog.Events.LogEventLevel Verbosity { get; set; }
    }

    [Verb("patch", HelpText = "Patch application and library.")]
    public class PatchOptions : BaseOption
    {
        [Option('t', "type", Default = PatchTypeEnum.BMW, HelpText = "Patch type, valid option: BMW, TOYOTA")]
        public PatchTypeEnum PatchType { get; set; }

        [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }

        [Option('e', "enable-enet", Default = false, HelpText = "Enable ENET programming")]
        public bool EnableENET { get; set; }

        [Option('f', "force", Default = false, HelpText = "Force patch application and library.")]
        public bool Force { get; set; }

        [Value(1, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
        public string TargetPath { get; set; }
    }

    [Verb("license", HelpText = "License related operations.")]
    public class LicenseOptions : BaseOption
    {
        [Option('g', "generate", HelpText = "Generate key pair", Group = "operation")]
        public bool GenerateKeyPair { get; set; }

        [Option('p', "patch", HelpText = "Patch target program", Group = "operation")]
        public string? TargetPath { get; set; }

        [Option('s', "sign", HelpText = "Sign license request", Group = "operation")]
        public bool SignLicense { get; set; }

        [Option('a', "auto", Default = false, HelpText = "Auto generate key pair and patch target program")]
        public bool AutoMode { get; set; }

        [Option('k', "key-pair", HelpText = "Path for key pair file")]
        public string? KeyPairPath { get; set; }

        [Option('l', "license", HelpText = "Path for license request file or base64 encoded content")]
        public string? LicenseRequestPath { get; set; }

        [Option('o', "output", HelpText = "Target path for signed license file")]
        public string? SignedLicensePath { get; set; }

        [Option('b', "base64", HelpText = "Base64 encoded")]
        public bool Base64 { get; set; }

        [Option('f', "force", Default = false, HelpText = "Force patch application and library.")]
        public bool Force { get; set; }

        [Option('d', "deobfuscate", Default = false, HelpText = "Deobfuscate application and library.")]
        public bool Deobfuscate { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt integrity checklist.")]
    public class DecryptOptions : BaseOption
    {
        [Value(0, MetaName = "ISTA-P path", Required = true, HelpText = "Path for ISTA-P")]
        public string? TargetPath { get; set; }
    }
}
