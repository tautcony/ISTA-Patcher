// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

using JetBrains.Annotations;

namespace ISTAPatcher.Commands.Options;

using DotMake.CommandLine;
using ISTAlter;

public interface ICommonPatchOption
{
    [UsedImplicitly]
    [CliOption(Description = "Restore the patched files to their original state.")]
    public bool Restore { get; set; }

    [UsedImplicitly]
    [CliOption(Description = "Specify the maximum degree of parallelism for patching.")]
    public int MaxDegreeOfParallelism { get; set; }

    [UsedImplicitly]
    [CliOption(Name = "--type", Description = "Specify the patch type.")]
    public ISTAOptions.PatchType PatchType { get; set; }

    [UsedImplicitly]
    [CliOption(Description = "Specify the mode type.")]
    public ISTAOptions.ModeType Mode { get; set; }

    [UsedImplicitly]
    [CliOption(Description = "Force patching on application and libraries.")]
    public bool Force { get; set; }

    [UsedImplicitly]
    [CliOption(Description = "Specify the libraries to skip patching.", Required = false)]
    public string[] SkipLibrary { get; set; }

    [UsedImplicitly]
    [CliArgument(Description = "Specify the path for ISTA.", Required = true, ValidationRules = CliValidationRules.ExistingDirectory)]
    public string? TargetPath { get; set; }
}
