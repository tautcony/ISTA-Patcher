// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAPatcher.Commands;

using DotMake.CommandLine;
using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Core.Patcher.Provider;
using ISTAlter.Utils;
using ISTAPatcher.Commands.Options;
using Serilog;

[CliCommand(
    Name="patch",
    Description = "Perform patching on application and libraries.",
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormAutoGenerate = false,
    Parent = typeof(RootCommand)
)]
public class PatchCommand : OptionalPatchOption, ICommonPatchOption
{
    public RootCommand? ParentCommand { get; set; }

    public bool Restore { get; set; }

    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    public ISTAOptions.PatchType PatchType { get; set; } = ISTAOptions.PatchType.B;

    public ISTAOptions.ModeType Mode { get; set; } = ISTAOptions.ModeType.Standalone;

    public bool Force { get; set; }

    public string[] SkipLibrary { get; set; } = [];

    public string? TargetPath { get; set; }

    [CliOption(Description = "Generate a registry file.")]
    public bool GenerateMockRegFile { get; set; }

    public void Run()
    {
        var opts = new ISTAOptions.PatchOptions
        {
            Verbosity = this.ParentCommand!.Verbosity,
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
