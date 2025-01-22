// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Utils;

using System.Text.RegularExpressions;
using ISTAlter.Models.iLean;

public partial class DataPackageUtils
{
    public static DataPackage DeterminePackageDetails(string fileName)
    {
        var versionPattern = fileName.Contains("ISPI_ICOM", StringComparison.Ordinal) ? ICOMVersionPattern() : VersionPattern();

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var nameWithoutVersion = versionPattern.Replace(nameWithoutExtension, string.Empty);
        var version = versionPattern.Match(fileName).Value.TrimStart('_');

        var ret = fileName switch
        {
            not null when fileName.Contains("AirClient", StringComparison.Ordinal) => ("AIR Client", "AIR", 1, string.Empty),
            not null when fileName.Contains("DCOM_Adapter_DCOM-ISPA4", StringComparison.Ordinal) => ("DCOM Adapter DCOM-ISPA4", "DCOM Adapter DCOM-ISPA4", 1, string.Empty),
            not null when fileName.Contains("DCOM_Adapter_DCOM-ISPA5", StringComparison.Ordinal) => ("DCOM Adapter DCOM-ISPA5", "DCOM Adapter DCOM-ISPA5", 1, string.Empty),
            not null when fileName.Contains("DCOM_Core", StringComparison.Ordinal) => ("DCOM Core", "DCOM Core", 1, string.Empty),
            not null when fileName.Contains("HDD-Update", StringComparison.Ordinal) => ("HDD-Update", "HDD Update", 1, string.Empty),
            not null when fileName.Contains("ISPI_ISVM_IMIB.MA", StringComparison.Ordinal) => ("IMIB MA", "IMIB Next", 1, string.Empty),
            not null when fileName.Contains("Admin_Client", StringComparison.Ordinal) => ("ISPI Admin Client", "ISPI Admin Client", 1, string.Empty),
            not null when fileName.Contains("ISTA-LAUNCHER_", StringComparison.Ordinal) => (nameWithoutVersion, "ISTA Launcher", 1, string.Empty),
            not null when IcomFwPattern().IsMatch(fileName) => (nameWithoutVersion, "ISTA Launcher", 2, string.Empty),
            not null when IstaMetaPattern().IsMatch(fileName) => (nameWithoutVersion, "ISTA Launcher", 2, string.Empty),
            not null when IstaMetaSdpPattern().IsMatch(fileName) => (nameWithoutExtension, "ISTA SDP", 1, string.Empty),
            not null when fileName.Contains("ISTA-BLP", StringComparison.Ordinal) => (nameWithoutExtension, "ISTA SDP BLP", 2, string.Empty),
            not null when IstaFullSdpPattern().IsMatch(fileName) => (fileName, "ISTA SDP", 2, string.Empty),
            not null when IstaSdpPattern().IsMatch(fileName) => (nameWithoutExtension, "ISTA SDP", 2, string.Empty),
            not null when fileName.Contains("ISTA-APP_", StringComparison.Ordinal) => (nameWithoutVersion, "ISTA NF", 1, string.Empty),
            not null when fileName.Contains("ISTA-DATA_", StringComparison.Ordinal) => (nameWithoutVersion, "ISTA NF", 2, string.Empty),
            not null when fileName.Contains("ISTA-P_SYS", StringComparison.Ordinal) => ("ISTA-P", "ISTA P", 1, $"ISTA-P_DAT - {version}"),
            not null when fileName.Contains("ISTA-P_DAT", StringComparison.Ordinal) => ("ISTA-P_DAT", "ISTA P", 2, string.Empty),
            _ => (null, null, 0, string.Empty),
        };
        return new DataPackage
        {
            Name = ret.Item1,
            System = ret.Item2,
            FileType = ret.Item3,
            Requires = ret.Item4,
            Version = version,
        };
    }

    [GeneratedRegex(@"_[\d.]+(?=\.|$)", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionPattern();

    [GeneratedRegex(@"_[\d\-]+(?=\.|$)", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ICOMVersionPattern();

    [GeneratedRegex(@"ICOM-FW|ICOM-Next-FW", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IcomFwPattern();

    [GeneratedRegex(@"ISTA-META(?!_SDP)", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IstaMetaPattern();

    [GeneratedRegex(@"ISTA_FULL-SDP_|ISTA-SDP_FULL_", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IstaFullSdpPattern();

    [GeneratedRegex(@"ISTA_DELTA-SDP_|ISTA-SDP_DELTA_|ISTA_SDP-MR_|ISTA_SDP-RSU_", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IstaSdpPattern();

    [GeneratedRegex(@"ISTA-META_SDP|ISTA-META_SDP-MR|ISTA-META_SDP-RSU", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IstaMetaSdpPattern();
}
