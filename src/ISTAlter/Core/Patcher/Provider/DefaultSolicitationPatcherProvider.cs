// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Core.Patcher.Provider;

public class DefaultSolicitationPatcherProvider : DefaultPatcherProvider
{
    public DefaultSolicitationPatcherProvider(string modulus, string exponent, ISTAOptions.CerebrumancyOptions opts)
        : base(opts)
    {
        this.Patches.Add(new PatchInfo(
            PatchUtils.PatchGetRSAPKCS1SignatureDeformatter(modulus, exponent),
            ((Delegate)PatchUtils.PatchGetRSAPKCS1SignatureDeformatter).Method,
            0
        ));
    }
}
