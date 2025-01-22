// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAlter.Models.iLean;

using System.Text.Json.Serialization;

public class DataPackage
{
    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("basedUpon")]
    public string? BasedUpon { get; set; }

    [JsonPropertyName("checkSum")]
    public string? CheckSum { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("isForced")]
    public bool IsForced { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("requires")]
    public string? Requires { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("type")]
    public int FileType { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /**
     * <summary>Gets or sets Alter state.</summary>
     * true - altered
     * false - exists
     * null - added
     */
    [JsonIgnore]
    public bool? Altered { get; set; } = false;
}
