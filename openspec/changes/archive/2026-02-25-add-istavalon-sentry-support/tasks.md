## 1. Dependency Alignment

- [x] 1.1 Add Sentry package references to `src/ISTAvalon/ISTAvalon.csproj` and align versions with the baseline used by `SentryTask`.
- [x] 1.2 Restore/update lock files for ISTAvalon and confirm resolved Sentry package graph is consistent.

## 2. Telemetry Bootstrap Implementation

- [x] 2.1 Add a dedicated telemetry bootstrap service in `src/ISTAvalon/Services/` to encapsulate `SentrySdk.Init` configuration.
- [x] 2.2 Configure bootstrap options to match `SentryTask` baseline (`Dsn`, `AutoSessionTracking`, `IsGlobalModeEnabled`, `SendDefaultPii`, `TracesSampleRate`, profiling integration, debug environment).
- [x] 2.3 Hook the telemetry bootstrap into ISTAvalon startup before main window creation and ensure it executes once per process.

## 3. Scope Context and Fail-Safe Behavior

- [x] 3.1 Add startup scope context payload (startup args or equivalent metadata) during telemetry configuration.
- [x] 3.2 Implement best-effort git metadata enrichment tags (username, email, branch, commit, remotes).
- [x] 3.3 Handle metadata discovery/configuration exceptions so startup continues and fallback marker is written when metadata is unavailable.

## 4. Verification

- [x] 4.1 Verify Debug build sets Sentry environment to `debug` and initializes before `MainWindow` is shown.
- [x] 4.2 Verify non-Debug build startup still succeeds with telemetry enabled and no duplicate initialization.
- [x] 4.3 Verify startup remains functional when git metadata discovery fails, with fallback marker present in scope/tags.
