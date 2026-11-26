// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023-2026 TautCony

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
        if (fileInfos == null || fileInfos.Count < 2)
        {
            throw new ArgumentException(
                $"Invalid file info format. Expected at least 2 elements, got {fileInfos?.Count ?? 0}",
                nameof(fileInfos));
        }

        this.FilePath = fileInfos[0].Trim('\uFEFF').Replace('\\', '/');
        this.FileName = Path.GetFileName(this.FilePath);
        this._hash = fileInfos[1];
    }

    public static Task<string> CalculateHash(string pathFile) => CalculateHash(pathFile, CancellationToken.None);

    public static async Task<string> CalculateHash(string pathFile, CancellationToken cancellationToken)
    {
        try
        {
            using var sha = SHA256.Create();
            await using var fileStream = File.OpenRead(pathFile);
            var hex = BitConverter.ToString(await sha.ComputeHashAsync(fileStream, cancellationToken).ConfigureAwait(false));
            return hex.Replace("-", string.Empty, StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or IOException)
        {
            Log.Warning(ex, "Failed to calculate hash for: {FileName}", pathFile);
            return string.Empty;
        }
    }
}
