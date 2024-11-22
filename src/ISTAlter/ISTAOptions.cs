// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter;

// ReSharper disable PropertyCanBeMadeInitOnly.Global, NotNullOrRequiredMemberIsNotInitialized
public class ISTAOptions
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

        public bool EnableFinishedOperations { get; set; }

        public bool DisableRequirementsCheck { get; set; }

        public bool EnableNotSend { get; set; }

        public bool SkipLicensePatch { get; set; }

        public bool EnableOffline { get; set; }

        public bool UserAuthEnv { get; set; }

        public string? MarketLanguage { get; set; }

        public bool SkipSyncClientConfig { get; set; }

        public int MaxDegreeOfParallelism { get; set; }

        public bool DisableFakeFSCReject { get; set; }
    }

    public class PatchOptions : OptionalPatchOptions
    {
        public PatchType PatchType { get; set; }

        public bool GenerateMockRegFile { get; set; }

        public bool Deobfuscate { get; set; }

        public bool Force { get; set; }

        public string[] SkipLibrary { get; set; }

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

    public class ILeanOptions : BaseOptions
    {
        public string? MachineGuid { get; set; }

        public string? VolumeSerialNumber { get; set; }

        public bool ShowMachineInfo { get; set; }

        public string? Encrypt { get; set; }

        public string? Decrypt { get; set; }
    }
}
