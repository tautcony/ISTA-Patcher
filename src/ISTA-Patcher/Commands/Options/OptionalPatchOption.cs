// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAPatcher.Commands.Options;

using DotMake.CommandLine;

public class OptionalPatchOption
{
    [CliOption(Name = "--enable-enet", Description = "Enable ENET programming functionality.")]
    public bool Enet { get; set; }

    [CliOption(Name = "--enable-finished-op", Description = "Enable to open finished operations functionality.")]
    public bool FinishedOperations { get; set; }

    [CliOption(Name = "--enable-skip-requirements-check", Description = "Enable skip the system requirements check functionality.")]
    public bool SkipRequirementsCheck { get; set; }

    [CliOption(Name = "--enable-data-not-send", Description = "Enable VIN Not Send Data functionality.")]
    public bool DataNotSend { get; set; }

    [CliOption(Name = "--enable-skip-sync-client-config", Description = "Enable skip sync client configuration functionality.")]
    public bool SkipSyncClientConfig { get; set; }

    [CliOption(Name = "--enable-skip-fake-fsc-reject", Description = "Enable skip fake FSC reject functionality.")]
    public bool SkipFakeFSCReject { get; set; }

    [CliOption(Name = "--enable-air-client", Description = "Enable AIR Client functionality.")]
    public bool AirClient { get; set; }

    [CliOption(Name = "--market-language", Description = "Specify the market language.", Required = false)]
    public string? MarketLanguage { get; set; }

    [CliOption(Name = "--patch-user-auth", Description = "Patch the user authentication environment.")]
    public bool UserAuthEnv { get; set; }
}
