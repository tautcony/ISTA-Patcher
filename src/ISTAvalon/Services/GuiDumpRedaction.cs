// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Text.RegularExpressions;
using ISTAvalon.Models;

/// <summary>
/// Redacts sensitive parameter values and log text for GUI state dumps.
/// </summary>
public static partial class GuiDumpRedaction
{
    private const string Redacted = "[REDACTED]";

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "MachineGuid",
        "VolumeSerialNumber",
        "VolumeSerial",
        "Secret",
        "Token",
        "ApiKey",
        "PrivateKey",
        "LicenseKey",
        "Mentacorrosion",
        "Solicitation",
        "LoadPrimamind",
        "Mentalysis",
    };

    public static bool IsSensitiveProperty(string propertyName) =>
        SensitivePropertyNames.Contains(propertyName) ||
        propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase);

    public static bool IsPathLike(ParameterKind kind, string propertyName) =>
        kind == ParameterKind.Path ||
        propertyName.Contains("Path", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Directory", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Folder", StringComparison.OrdinalIgnoreCase);

    public static object? RedactValue(string propertyName, ParameterKind kind, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (IsSensitiveProperty(propertyName))
        {
            return value switch
            {
                string { Length: 0 } => null,
                string => Redacted,
                string[] arr => arr.Length == 0 ? null : arr.Select(_ => Redacted).ToArray(),
                _ => Redacted,
            };
        }

        if (IsPathLike(kind, propertyName))
        {
            return value switch
            {
                string { Length: 0 } => null,
                string s => RedactPath(s),
                string[] arr => arr.Length == 0 ? null : arr.Select(RedactPath).ToArray(),
                _ => value,
            };
        }

        return value;
    }

    public static string RedactPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            var fileName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(fileName))
            {
                return "[REDACTED_PATH]";
            }

            return $"[REDACTED_PATH]/{fileName}";
        }
        catch
        {
            return "[REDACTED_PATH]";
        }
    }

    public static string RedactLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var redacted = SensitiveAssignmentPattern().Replace(message, "$1" + Redacted);
        redacted = SensitiveLabelPattern().Replace(redacted, "$1" + Redacted);
        return redacted;
    }

    [GeneratedRegex(
        @"(?i)\b(password|machineguid|volumeserialnumber|volumeserial|secret|token|apikey|privatekey|licensekey)\s*[:=]\s*\S+",
        RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveAssignmentPattern();

    [GeneratedRegex(
        @"(?i)\b(password|machineguid|volumeserialnumber|volumeserial|secret|token)\b(\s+)" + @"\S+",
        RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveLabelPattern();
}
