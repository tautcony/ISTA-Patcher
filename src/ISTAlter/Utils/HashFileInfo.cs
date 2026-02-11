// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2024 TautCony

namespace ISTAlter.Utils;

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
                var hex = BitConverter.ToString(bytes);
                this._hash = hex.Replace("-", string.Empty, StringComparison.Ordinal);
            }
            catch (FormatException ex)
            {
                SentrySdk.CaptureException(ex);
                Log.Warning(ex, "Failed to parse hash value [{Hash}] for: {FileName}", this._hash, this.FileName);
                this._hash = string.Empty;
            }

            this._decoded = true;
            return this._hash;
        }
    }

    private string _hash;
    private bool _decoded;

    protected internal HashFileInfo(IReadOnlyList<string> fileInfos)
    {
        this.FilePath = fileInfos[0].Trim('\uFEFF').Replace('\\', '/');
        this.FileName = Path.GetFileName(this.FilePath);
        this._hash = fileInfos[1];
    }

    public static async Task<string> CalculateHash(string pathFile)
    {
        try
        {
            using var sha = SHA256.Create();
            await using var fileStream = File.OpenRead(pathFile);
            var hex = BitConverter.ToString(await sha.ComputeHashAsync(fileStream).ConfigureAwait(false));
            return hex.Replace("-", string.Empty, StringComparison.Ordinal);
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning(ex, "Failed to calculate hash for: {FileName}", pathFile);
            return string.Empty;
        }
    }
}
