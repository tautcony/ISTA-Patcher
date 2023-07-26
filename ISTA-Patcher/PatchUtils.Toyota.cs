// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher;

using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the patching logic for toyota(WIP).
/// </summary>
internal static partial class PatchUtils
{
    [ToyotaPatch]
    public static int PatchToyotaWorker(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.Toyota.Worker.ToyotaWorker",
            "VehicleIsValid",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ToyotaPatch]
    public static int PatchIndustrialCustomerManager(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.IndustrialCustomer.Manager.IndustrialCustomerManager",
            "IsIndustrialCustomerBrand",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ToyotaPatch]
    public static int PatchGTSLicenseManager(AssemblyDefinition assembly)
    {
        var typeDef = assembly.Modules.SelectMany(m => m.GetTypes())
                         .FirstOrDefault(tp => tp.FullName == "Toyota.GTS.ForIsta.GTSLicenseManager");
        if (typeDef == null)
        {
            return 0;
        }

        var licenseStatus = DnlibUtils.FindPropertyInClassAndBaseClasses(typeDef, "LicenseStatus");
        if (licenseStatus == null)
        {
            return 0;
        }

        licenseStatus.GetMethod.ReturnZeroMethod();
        return 1;
    }
}
