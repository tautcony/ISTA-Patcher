// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTA_Patcher.Tasks;

using LibGit2Sharp;
using Serilog;

public class SentryTask : IStartupTask
{
    public void Execute()
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
        SentrySdk.ConfigureScope(scope =>
        {
            try
            {
                var repoPath = Repository.Discover(AppDomain.CurrentDomain.BaseDirectory);
                using var repo = new Repository(repoPath);
                var username = repo.Config.Get<string>("user.name");
                var email = repo.Config.Get<string>("user.email");
                scope.SetTag("git.username", username.Value);
                scope.SetTag("git.email", email.Value);
                scope.SetTag("git.branch", repo.Head.FriendlyName);
                scope.SetTag("git.commit", repo.Head.Tip.Sha);
                foreach (var remote in repo.Network.Remotes)
                {
                    scope.SetTag($"git.remote.{remote.Name}", remote.Url);
                }
            }
            catch (TypeInitializationException)
            {
                // ignored
            }
            catch (LibGit2SharpException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize VSC Config");
            }
        });
    }
}
