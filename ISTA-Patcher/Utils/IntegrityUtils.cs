// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable InconsistentNaming, StringLiteralTypo, CommentTypo, UseUtf8StringLiteral
namespace ISTA_Patcher.Utils;

using System.Security.Cryptography;
using System.Text;
using Serilog;

public static class IntegrityUtils
{
    private static readonly byte[] _salt = { 0xd, 0xca, 0x32, 0xe0, 0x7f, 0xa4, 0xdf, 0xf1 };

    private const int _iterations = 1100;

    private static readonly byte[] _password =
    {
        0x33, 0x2f, 0x33, 0x48, 0x65, 0x78, 0x62, 0x4b, 0x4b, 0x46, 0x73, 0x34, 0x4c, 0x71, 0x70, 0x69,
        0x43, 0x53, 0x67, 0x4b, 0x41, 0x58, 0x47, 0x55, 0x59, 0x43, 0x74, 0x71, 0x6a, 0x6f, 0x46, 0x63,
        0x68, 0x66, 0x50, 0x69, 0x74, 0x41, 0x6d, 0x49, 0x38, 0x77, 0x45, 0x3d,
    };

    public static List<HashFileInfo>? DecryptFile(string sourceFilename)
    {
        try
        {
            var aesManaged = Aes.Create();
            aesManaged.BlockSize = aesManaged.LegalBlockSizes[0].MaxSize;
            aesManaged.KeySize = aesManaged.LegalKeySizes[0].MaxSize;

            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(_password, _salt, _iterations, HashAlgorithmName.SHA1);

            aesManaged.Key = rfc2898DeriveBytes.GetBytes(aesManaged.KeySize / 8);
            aesManaged.IV = rfc2898DeriveBytes.GetBytes(aesManaged.BlockSize / 8);
            aesManaged.Mode = CipherMode.CBC;
            var transform = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
            using (var fileStream = new FileStream(sourceFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.CopyTo(cryptoStream);
            }

            var bytes = memoryStream.ToArray();
            return (from row in Encoding.UTF8.GetString(bytes).Split(";;\r\n", StringSplitOptions.RemoveEmptyEntries).Distinct()
                select new HashFileInfo(row.Split(";;", StringSplitOptions.RemoveEmptyEntries))).ToList();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to decrypt file: {Reason}", ex.Message);
        }

        return null;
    }
}
