// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Commands;

using DotMake.CommandLine;
using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Core.Patcher.Provider;
using ISTAlter.Utils;
using Serilog;
using Serilog.Events;

[CliCommand(
    Name="patch",
    Description = "Perform patching on application and libraries.",
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormAutoGenerate = false,
    Parent = typeof(RootCommand)
)]
public class PatchCommand : OptionalCommandBase
{
    [CliOption(Description = "Specify the verbosity level of the output.")]
    public LogEventLevel Verbosity { get; set; } = LogEventLevel.Information;

    [CliOption(Description = "Restore the patched files to their original state.")]
    public bool Restore { get; set; }

    [CliOption(Description = "Specify the mode type.")]
    public ISTAOptions.ModeType Mode { get; set; } = ISTAOptions.ModeType.Standalone;

    [CliOption(Description = "Specify the maximum degree of parallelism for patching.")]
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    [CliOption(Name="--type", Description = "Specify the patch type.")]
    public ISTAOptions.PatchType PatchType { get; set; } = ISTAOptions.PatchType.B;

    [CliOption(Description = "Generate a registry file.")]
    public bool GenerateMockRegFile { get; set; }

    [CliOption(Description = "Force patching on application and libraries.")]
    public bool Force { get; set; }

    [CliOption(Description = "Specify the libraries to skip patching.")]
    public string[] SkipLibrary { get; set; } = [];

    [CliArgument(Description = "Specify the path for ISTA-P.", Required = true)]
    public string? TargetPath { get; set; }

    public void Run()
    {
        var opts = new ISTAOptions.PatchOptions
        {
            Verbosity = this.Verbosity,
            Restore = this.Restore,
            ENET = this.Enet,
            FinishedOperations = this.FinishedOperations,
            SkipRequirementsCheck = this.SkipRequirementsCheck,
            DataNotSend = this.DataNotSend,
            Mode = this.Mode,
            UserAuthEnv = this.UserAuthEnv,
            MarketLanguage = this.MarketLanguage,
            SkipSyncClientConfig = this.SkipSyncClientConfig,
            MaxDegreeOfParallelism = this.MaxDegreeOfParallelism,
            SkipFakeFSCReject = this.SkipFakeFSCReject,
            AirClient = this.AirClient,
            PatchType = this.PatchType,
            GenerateMockRegFile = this.GenerateMockRegFile,
            Force = this.Force,
            SkipLibrary = this.SkipLibrary,
            TargetPath = this.TargetPath,
        };

        Execute(opts).Wait();
    }

    public static Task<int> Execute(ISTAOptions.PatchOptions opts)
    {
        using var transaction = new TransactionHandler("ISTA-Patcher", "patch");
        opts.Transaction = transaction;
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;

        var guiBasePath = Constants.TesterGUIPath.Aggregate(opts.TargetPath, Path.Join);
        var psdzBasePath = Constants.PSdZPath.Aggregate(opts.TargetPath, Path.Join);

        if (!Directory.Exists(guiBasePath) || !Directory.Exists(psdzBasePath))
        {
            Log.Fatal("Folder structure does not match under: {TargetPath}, please check options", opts.TargetPath);
            return Task.FromResult(-1);
        }

        IPatcherProvider patcherProvider = opts.PatchType switch
        {
            ISTAOptions.PatchType.B => new DefaultPatcherProvider(opts),
            ISTAOptions.PatchType.T => new ToyotaPatcherProvider(),
            _ => throw new NotSupportedException(),
        };

        Patch.PatchISTA(patcherProvider, opts);
        return Task.FromResult(0);
    }
}
