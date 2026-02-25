// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using LibGit2Sharp;
using Serilog;

public static class TelemetryBootstrap
{
    private static int _initialized;

    public static void Initialize(string[] startupArgs)
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
        {
            return;
        }

        SentrySdk.Init(options =>
        {
            options.Dsn = "https://55e58df747fc4d43912790aa894700ba@o955448.ingest.sentry.io/4504370799116288";
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.SendDefaultPii = true;
            options.TracesSampleRate = 1;
            options.AddIntegration(new Sentry.Profiling.ProfilingIntegration());
#if DEBUG
            options.Environment = "debug";
#endif
        });

        SentrySdk.ConfigureScope(scope =>
        {
            scope.Contexts["Startup"] = new
            {
                Parameters = startupArgs,
            };

            try
            {
                var repoPath = Repository.Discover(AppDomain.CurrentDomain.BaseDirectory);
                if (repoPath == null)
                {
                    scope.SetTag("git.not_found", "true");
                    return;
                }

                using var repo = new Repository(repoPath);
                var username = repo.Config.Get<string>("user.name");
                var email = repo.Config.Get<string>("user.email");
                scope.SetTag("git.username", username?.Value ?? string.Empty);
                scope.SetTag("git.email", email?.Value ?? string.Empty);
                scope.SetTag("git.branch", repo.Head.FriendlyName);
                scope.SetTag("git.commit", repo.Head.Tip.Sha);

                foreach (var remote in repo.Network.Remotes)
                {
                    scope.SetTag($"git.remote.{remote.Name}", remote.Url);
                }
            }
            catch (TypeInitializationException)
            {
                scope.SetTag("git.not_found", "true");
            }
            catch (LibGit2SharpException)
            {
                scope.SetTag("git.not_found", "true");
            }
            catch (ArgumentNullException)
            {
                scope.SetTag("git.not_found", "true");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize Sentry git metadata");
                scope.SetTag("git.not_found", "true");
            }
        });
    }
}
