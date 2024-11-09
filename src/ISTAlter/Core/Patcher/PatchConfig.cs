// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Core.Patcher;

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
internal sealed partial class PatchConfigSourceGenerationContext : JsonSerializerContext;
