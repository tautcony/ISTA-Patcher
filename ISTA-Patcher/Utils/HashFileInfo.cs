// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Utils;

using System.Security.Cryptography;
using Serilog;

public class HashFileInfo
{
    public string FileName { get; }

    public string FilePath { get; }

    public string Hash
    {
        get
        {
            if (this._decoded)
            {
                return this._hash;
            }

            try
            {
                var bytes = Convert.FromBase64String(this._hash);
                var hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
                this._hash = hex;
            }
            catch (FormatException ex)
            {
                this._hash = string.Empty;
                Log.Warning(ex, "Failed to parse hash value [{Hash}] for: {FileName}", this._hash, this.FileName);
            }

            this._decoded = true;
            return this._hash;
        }
    }

    private string _hash;
    private bool _decoded;

    protected internal HashFileInfo(IReadOnlyList<string> fileInfos)
    {
        this.FilePath = fileInfos[0].Trim('\uFEFF').Replace("\\", "/");
        this.FileName = Path.GetFileName(this.FilePath ?? string.Empty);
        this._hash = fileInfos[1];
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
