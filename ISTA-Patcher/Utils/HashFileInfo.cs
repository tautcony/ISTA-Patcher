// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Utils;

using System.Security.Cryptography;
using Serilog;

public class HashFileInfo
{
    public string FileName { get; }

    public string FilePath { get; }

    public string Hash { get; }

    protected internal HashFileInfo(IReadOnlyList<string> fileInfos)
    {
        this.FilePath = fileInfos[0].Trim('\uFEFF').Replace("\\", "/");
        this.FileName = Path.GetFileName(this.FilePath ?? string.Empty).Trim('\uFEFF');
        try
        {
            var bytes = Convert.FromBase64String(fileInfos[1]);
            var hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
            this.Hash = hex;
        }
        catch (FormatException ex)
        {
            this.Hash = string.Empty;
            Log.Warning(ex, "Failed to parse hash value [{Hash}] for: {FileName}", fileInfos[1], this.FileName);
        }
    }

    public static string CalculateHash(string pathFile)
    {
        try
        {
            using var sha = SHA256.Create();
            using var fileStream = File.OpenRead(pathFile);
            var text = BitConverter.ToString(sha.ComputeHash(fileStream)).Replace("-", string.Empty);
            return text;
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning(ex, "Failed to calculate hash for: {FileName}", pathFile);
            return string.Empty;
        }
    }
}
