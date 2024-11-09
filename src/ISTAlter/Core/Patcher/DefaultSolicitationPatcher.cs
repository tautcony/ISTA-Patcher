// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Core.Patcher;

public class DefaultSolicitationPatcher : DefaultPatcher
{
    public DefaultSolicitationPatcher(string modulus, string exponent, ISTAOptions.CerebrumancyOptions opts)
        : base(opts)
    {
        this.Patches.Add((
            PatchUtils.PatchGetRSAPKCS1SignatureDeformatter(modulus, exponent),
            ((Delegate)PatchUtils.PatchGetRSAPKCS1SignatureDeformatter).Method
        ));
    }
}
