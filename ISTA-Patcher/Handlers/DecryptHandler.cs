// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using ConsoleTables;
using ISTA_Patcher.Utils;
using Serilog;

public static class DecryptHandler
{
    public static async Task<int> Execute(ProgramArgs.DecryptOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;
        var encryptedFileList = Utils.Constants.EncCnePath.Aggregate(opts.TargetPath, Path.Join);
        var basePath = Path.Join(opts.TargetPath, Utils.Constants.TesterGUIPath[0]);
        if (!File.Exists(encryptedFileList))
        {
            Log.Error("File {FilePath} does not exist", encryptedFileList);
            return -1;
        }

        var fileList = IntegrityUtils.DecryptFile(encryptedFileList);
        if (fileList == null)
        {
            return -1;
        }

        var table = new ConsoleTable("FilePath", "Hash(SHA256)", "Integrity");

        foreach (var fileInfo in fileList)
        {
            if (opts.Integrity)
            {
                var checkResult = await CheckFileIntegrity(basePath, fileInfo).ConfigureAwait(false);
                var info = string.IsNullOrEmpty(checkResult.Value) ? fileInfo.FilePath : $"{fileInfo.FilePath} ({checkResult.Value})";
                table.AddRow(info, fileInfo.Hash, checkResult.Key);
            }
            else
            {
                table.AddRow(fileInfo.FilePath, fileInfo.Hash, "/");
            }
        }

        Log.Information("Markdown result:{NewLine}{Markdown}", Environment.NewLine, table.ToMarkDownString());
        return 0;
    }

    private static async Task<KeyValuePair<string, string>> CheckFileIntegrity(string basePath, HashFileInfo fileInfo)
    {
        string? checkResult;
        var version = string.Empty;
        var filePath = Path.Join(basePath, fileInfo.FilePath);
        if (!File.Exists(filePath))
        {
            return new KeyValuePair<string, string>("Not Found", string.Empty);
        }

        try
        {
            var module = Core.PatchUtils.LoadModule(filePath);
            version = module.Assembly.Version.ToString();
        }
        catch (System.BadImageFormatException)
        {
            Log.Warning("None .NET assembly found: {FilePath}", filePath);
        }

        if (fileInfo.Hash == string.Empty)
        {
            checkResult = "[EMPTY]";
        }
        else
        {
            var realHash = await HashFileInfo.CalculateHash(filePath).ConfigureAwait(false);
            checkResult = string.Equals(realHash, fileInfo.Hash, StringComparison.Ordinal) ? "[OK]" : "[NG]";
        }

        if (!OperatingSystem.IsWindows())
        {
            return new KeyValuePair<string, string>(checkResult, version);
        }

        var wasVerified = false;

        var bChecked = NativeMethods.StrongNameSignatureVerificationEx(filePath, fForceVerification: true, ref wasVerified);
        if (bChecked)
        {
            checkResult += wasVerified ? "[S:OK]" : "[S:NG]";
        }
        else
        {
            checkResult += "[S:NF]";
        }

        return new KeyValuePair<string, string>(checkResult, version);
    }
}
