// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAlter.Models.iLean;

using System.Text.Json.Serialization;

public class DataPackageV06 : DataPackageV02
{
    [JsonPropertyName("priority")]
    public DownloadPriority Priority { get; set; }
}
