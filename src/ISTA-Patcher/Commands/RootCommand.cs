// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAPatcher.Commands;

using DotMake.CommandLine;
using Serilog.Events;

[CliCommand(
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormAutoGenerate = false
)]
public class RootCommand
{
    [CliOption(Description = "Specify the verbosity level of the output.")]
    public Serilog.Events.LogEventLevel Verbosity { get; set; } = LogEventLevel.Information;
}
