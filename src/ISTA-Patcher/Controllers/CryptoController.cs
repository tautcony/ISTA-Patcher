// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025-2026 TautCony

namespace ISTAPatcher.Controllers;

using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using ISTAPatcher.Commands;
using Org.BouncyCastle.Security;

public class CryptoController
{
    [ResourceMethod]
    public Result<Stream> CreateKeyPair()
    {
        var memoryStream = new MemoryStream();
        CryptoCommand.GenerateKeyStore().Save(memoryStream, CryptoCommand.KeyPairConfig.Select(i => (char)i).ToArray(), new SecureRandom());
        memoryStream.Position = 0;
        return new Result<Stream>(memoryStream).Header("Content-Disposition", "attachment; filename=\"keyContainer.pfx\"");
    }
}
