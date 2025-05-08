// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAPatcher.Controllers;

using ISTAlter.Core.iLean;

public class iLeanController
{
    public string Encrypt(string machineGuid, string volumeSerialNumber, string content)
    {
        using var encryption = new iLeanCipher(machineGuid, volumeSerialNumber);
        return encryption.Encrypt(content);
    }

    public string Decrypt(string machineGuid, string volumeSerialNumber, string content)
    {
        using var encryption = new iLeanCipher(machineGuid, volumeSerialNumber);
        return encryption.Decrypt(content);
    }
}
