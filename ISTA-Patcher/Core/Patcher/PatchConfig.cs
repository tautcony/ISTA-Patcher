// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Core.Patcher;

using System.Text.Json.Serialization;

public class PatchConfig
{
    [JsonPropertyName("include")]
    public required string[] Include { get; set; }

    [JsonPropertyName("exclude")]
    public required string[] Exclude { get; set; }

    [JsonPropertyName("include.toyota")]
    public string[]? IncludeToyota { get; set; }

    [JsonPropertyName("exclude.toyota")]
    public string[]? ExcludeToyota { get; set; }
}

[JsonSerializable(typeof(PatchConfig))]
internal partial class PatchConfigSourceGenerationContext : JsonSerializerContext;
