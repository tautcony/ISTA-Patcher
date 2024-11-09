// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Core;

using dnlib.DotNet;
using ISTAlter.Utils;

/// <summary>
/// A utility class for patching files and directories.
/// Contains the patching logic for toyota(WIP).
/// </summary>
public static partial class PatchUtils
{
    [ToyotaPatch]
    public static int PatchToyotaWorker(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.\u0054\u006f\u0079\u006f\u0074\u0061.Worker.ToyotaWorker",
            "VehicleIsValid",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ToyotaPatch]
    public static int PatchIndustrialCustomerManager(ModuleDefMD module)
    {
        return module.PatchFunction(
            "\u0042\u004d\u0057.Rheingold.CoreFramework.IndustrialCustomer.Manager.IndustrialCustomerManager",
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
