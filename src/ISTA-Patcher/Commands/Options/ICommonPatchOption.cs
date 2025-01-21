// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAPatcher.Commands.Options;

using DotMake.CommandLine;
using ISTAlter;

public interface ICommonPatchOption
{
    [CliOption(Description = "Restore the patched files to their original state.")]
    public bool Restore { get; set; }

    [CliOption(Description = "Specify the maximum degree of parallelism for patching.")]
    public int MaxDegreeOfParallelism { get; set; }

    [CliOption(Name = "--type", Description = "Specify the patch type.")]
    public ISTAOptions.PatchType PatchType { get; set; }

    [CliOption(Description = "Specify the mode type.")]
    public ISTAOptions.ModeType Mode { get; set; }

    [CliOption(Description = "Force patching on application and libraries.")]
    public bool Force { get; set; }

    [CliOption(Description = "Specify the libraries to skip patching.")]
    public string[] SkipLibrary { get; set; }

    [CliArgument(Description = "Specify the path for ISTA.", Required = true)]
    public string? TargetPath { get; set; }
}
