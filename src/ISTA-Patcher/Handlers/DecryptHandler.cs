// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Handlers;

using System.Drawing;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Utils;
using Serilog;

public static class DecryptHandler
{
    public static async Task<int> Execute(ISTAOptions.DecryptOptions opts)
    {
        Global.LevelSwitch.MinimumLevel = opts.Verbosity;
        var encryptedFileList = ISTAlter.Utils.Constants.EncCnePath.Aggregate(opts.TargetPath, Path.Join);
        var basePath = Path.Join(opts.TargetPath, ISTAlter.Utils.Constants.TesterGUIPath[0]);
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

        var headerFormat = new CellFormat
        {
            Alignment = Alignment.Center,
            ForegroundColor = Color.Chocolate,
        };

        var table = new TableBuilder(headerFormat)
            .AddColumn("FilePath").RowsFormat()
            .ForegroundColor(Color.Teal)
            .AddColumn("Hash(SHA256)").RowsFormat()
            .ForegroundColor(Color.Aqua)
            .AddColumn("Integrity").RowsFormat()
            .ForegroundColor(Color.DarkTurquoise)
            .Alignment(Alignment.Center)
            .Build();
        table.Config = TableConfig.Unicode();

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

        Log.Information("Result:{NewLine}{Table}", Environment.NewLine, table.ToString());
        return 0;
    }

    private static async Task<KeyValuePair<string, string>> CheckFileIntegrity(string basePath, HashFileInfo fileInfo)
    {
        const string checkNF = "[404]";
        const string checkOK = "[OK]";
        const string checkNG = "[NG]";
        const string checkSignOK = "[SIGN:OK]";
        const string checkSignNG = "[SIGN:NG]";
        const string checkSignNF = "[SIGN:404]";

        string? checkResult;
        var version = string.Empty;
        var filePath = Path.Join(basePath, fileInfo.FilePath);
        if (!File.Exists(filePath))
        {
            return new KeyValuePair<string, string>(checkNF, string.Empty);
        }

        try
        {
            var module = PatchUtils.LoadModule(filePath);
            version = module.Assembly.Version.ToString();
        }
        catch (System.BadImageFormatException)
        {
            Log.Warning("This file does not contain a managed assembly: {FilePath}", filePath);
        }

        if (fileInfo.Hash == string.Empty)
        {
            checkResult = "[EMPTY]";
        }
        else
        {
            var realHash = await HashFileInfo.CalculateHash(filePath).ConfigureAwait(false);
            checkResult = string.Equals(realHash, fileInfo.Hash, StringComparison.Ordinal) ? checkOK : checkNG;
        }

        if (OperatingSystem.IsWindows())
        {
            var wasVerified = false;

            var bChecked = NativeMethods.StrongNameSignatureVerificationEx(filePath, fForceVerification: true, ref wasVerified);
            if (bChecked)
            {
                checkResult += wasVerified ? checkSignOK : checkSignNG;
            }
            else
            {
                checkResult += checkSignNF;
            }
        }

        return new KeyValuePair<string, string>(checkResult, version);
    }
}
