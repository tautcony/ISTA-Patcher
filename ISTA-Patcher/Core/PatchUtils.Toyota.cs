// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

namespace ISTA_Patcher.Core;

using dnlib.DotNet;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the patching logic for toyota(WIP).
/// </summary>
internal static partial class PatchUtils
{
    [ToyotaPatch]
    public static int PatchToyotaWorker(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.Toyota.Worker.ToyotaWorker",
            "VehicleIsValid",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ToyotaPatch]
    public static int PatchIndustrialCustomerManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "BMW.Rheingold.CoreFramework.IndustrialCustomer.Manager.IndustrialCustomerManager",
            "IsIndustrialCustomerBrand",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ToyotaPatch]
    public static int PatchGTSLicenseManager(ModuleDefMD module)
    {
        return module.PatcherGetter(
            "Toyota.GTS.ForIsta.GTSLicenseManager",
            "LicenseStatus",
            DnlibUtils.ReturnZeroMethod);
    }
}
