// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Reflection;
using ISTAvalon.Models;

public static class AppMetadataProvider
{
    private const string DefaultProductName = "ISTA-Patcher";
    private const string DefaultRepositoryName = "tautcony/ISTA-Patcher";
    private const string DefaultRepositoryUrl = "https://github.com/tautcony/ISTA-Patcher";
    private const string DefaultLicenseIdentifier = "GPL-3.0-or-later";
    private const string DevVersion = "dev";

    public static AppMetadata Get()
    {
        var assembly = typeof(AppMetadataProvider).Assembly;
        var productName = GetTitle(assembly);
        var version = ResolveVersion(assembly);
        var repositoryUrl = GetAssemblyMetadata(assembly, "RepositoryUrl") ?? DefaultRepositoryUrl;
        var license = GetAssemblyMetadata(assembly, "PackageLicenseExpression") ?? DefaultLicenseIdentifier;

        return new AppMetadata(
            productName,
            version,
            DefaultRepositoryName,
            repositoryUrl,
            license);
    }

    private static string GetTitle(Assembly assembly)
    {
        return assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? DefaultProductName;
    }

    private static string ResolveVersion(Assembly assembly)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        var assemblyVersion = assembly.GetName().Version?.ToString();
        if (!string.IsNullOrWhiteSpace(assemblyVersion))
        {
            return assemblyVersion;
        }

        return DevVersion;
    }

    private static string? GetAssemblyMetadata(Assembly assembly, string key)
    {
        return assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => string.Equals(attr.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }
}
