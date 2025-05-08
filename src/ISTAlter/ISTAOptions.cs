// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAlter;

using ISTAlter.Utils;

// ReSharper disable PropertyCanBeMadeInitOnly.Global, NotNullOrRequiredMemberIsNotInitialized
public static class ISTAOptions
{
    public enum PatchType
    {
        B = 0,
        T = 1,
    }

    public enum ModeType
    {
        Standalone = 0,
        iLean = 1,
    }

    public enum CipherType
    {
        DefaultCipher = 0,
        PasswordCipher = 1,
    }

    public enum FormatterType
    {
        Default = 0,
        JSON = 1,
        XML = 2,
    }

    public class BaseOptions
    {
        public Serilog.Events.LogEventLevel Verbosity { get; set; }

        public TransactionHandler Transaction { get; set; }

        public bool Restore { get; set; }

        public string[]? Include { get; set; }

        public string[]? Exclude { get; set; }
    }

    public class OptionalPatchOptions : BaseOptions
    {
        public bool ENET { get; set; }

        public bool FinishedOperations { get; set; }

        public bool SkipRequirementsCheck { get; set; }

        public bool DataNotSend { get; set; }

        public ModeType Mode { get; set; }

        public bool UserAuthEnv { get; set; }

        public string? MarketLanguage { get; set; }

        public bool SkipSyncClientConfig { get; set; }

        public int MaxDegreeOfParallelism { get; set; }

        public bool SkipFakeFSCReject { get; set; }

        public bool AirClient { get; set; }

        public bool SkipBrandCompatibleCheck { get; set; }

        public bool FixDS2VehicleIdentification { get; set; }

        public bool ForceICOMNext { get; set; }
    }

    public class PatchOptions : OptionalPatchOptions
    {
        public PatchType PatchType { get; set; }

        public bool GenerateMockRegFile { get; set; }

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
    }

    public class CryptoOptions : BaseOptions
    {
        public bool Decrypt { get; set; }

        public bool Integrity { get; set; }

        public string? TargetPath { get; set; }

        public bool CreateKeyPair { get; set; }
    }

    public class ILeanOptions : BaseOptions
    {
        public CipherType CipherType { get; set; }

        public string? MachineGuid { get; set; }

        public string? VolumeSerialNumber { get; set; }

        public string? Password { get; set; }

        public bool ShowMachineInfo { get; set; }

        public string? Encrypt { get; set; }

        public string? Decrypt { get; set; }

        public string? Output { get; set; }

        public FormatterType Formatter { get; set; }
    }
}
