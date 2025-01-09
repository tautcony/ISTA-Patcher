// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAPatcher.Handlers;

using ISTAlter;
using ISTAlter.Core;
using ISTAlter.Utils;
using Serilog;
using Spectre.Console;

public static class DecryptHandler
{
    public static async Task<int> Execute(ISTAOptions.DecryptOptions opts)
    {
        using var transaction = new TransactionHandler("ISTA-Patcher", "decrypt");
        opts.Transaction = transaction;
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

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn(new TableColumn("[u]FilePath[/]").NoWrap())
            .AddColumn(new TableColumn("[u]Hash(SHA256)[/]").NoWrap())
            .AddColumn(new TableColumn("[u]Integrity[/]").NoWrap());

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

        AnsiConsole.Write(table);
        return 0;
    }

    private static async Task<KeyValuePair<string, string>> CheckFileIntegrity(string basePath, HashFileInfo fileInfo)
    {
        const string checkNG = "[red]NG[/]";
        const string checkNF = "[yellow]404[/]";
        const string checkOK = "[green]OK[/]";
        const string checkEmpty = "[gray]???[/]";
        const string checkSignNG = "|[red]SIGN:NG[/]";
        const string checkSignNF = "|[yellow]SIGN:404[/]";
        const string checkSignOK = "|[green]SIGN:OK[/]";

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
            checkResult = checkEmpty;
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
