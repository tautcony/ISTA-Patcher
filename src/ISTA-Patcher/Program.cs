// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher;

using ISTA_Patcher.Handlers;
using LibGit2Sharp;
using Sentry.Profiling;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://55e58df747fc4d43912790aa894700ba@o955448.ingest.sentry.io/4504370799116288";
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.SendDefaultPii = true;
            options.TracesSampleRate = 1;
            options.AddIntegration(new ProfilingIntegration());
#if DEBUG
            options.Environment = "debug";
#endif
        });
        Log.Logger = new LoggerConfiguration()
                     .Enrich.FromLogContext()
                     .MinimumLevel.ControlledBy(Global.LevelSwitch)
                     .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                     .WriteTo.File("ista-patcher.log", rollingInterval: RollingInterval.Day)
                     .WriteTo.Sentry(LogEventLevel.Error, LogEventLevel.Debug)
                     .CreateLogger();

        var repoPath = Repository.Discover(AppDomain.CurrentDomain.BaseDirectory);
        if (repoPath != null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                try
                {
                    using var repo = new Repository(repoPath);
                    var username = repo.Config.Get<string>("user.name");
                    var email = repo.Config.Get<string>("user.email");
                    scope.SetTag("git.username", username.Value);
                    scope.SetTag("git.email", email.Value);
                    scope.SetTag("git.branch", repo.Head.FriendlyName);
                    scope.SetTag("git.commit", repo.Head.Tip.Sha);
                    if (repo.Network.Remotes["origin"] != null)
                    {
                        scope.SetTag("git.remote", repo.Network.Remotes["origin"].Url);
                    }
                }
                catch (LibGit2SharpException)
                {
                    // ignore
                }
            });
        }

        var command = ProgramArgs.BuildCommandLine(
            PatchHandler.Execute,
            CerebrumancyHandler.Execute,
            DecryptHandler.Execute,
            iLeanHandler.Execute);

        var parseResult = command.Parse(args);
        var transaction = SentrySdk.StartTransaction("ISTA-Patcher", parseResult.CommandResult.Command.ToString());
        var ret = parseResult.InvokeAsync();
        transaction.Finish();
        return ret;
    }
}
