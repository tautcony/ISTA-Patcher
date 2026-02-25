// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Models;

public sealed record AppMetadata(
    string ProductName,
    string Version,
    string RepositoryName,
    string RepositoryUrl,
    string LicenseIdentifier)
{
    public string WindowTitle => $"{ProductName} v{Version}";
}
