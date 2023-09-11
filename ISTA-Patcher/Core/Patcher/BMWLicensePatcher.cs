// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Core.Patcher;

public class BMWLicensePatcher : BMWPatcher
{
    public BMWLicensePatcher(string modulus, string exponent, ProgramArgs.LicenseOptions opts)
        : base(opts)
    {
        this.Patches.Add(
            PatchUtils.PatchGetRSAPKCS1SignatureDeformatter(modulus, exponent)
        );
    }
}
