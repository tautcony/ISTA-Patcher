## Purpose

Describe ISTAvalon Sentry initialization and scope enrichment as implemented by `TelemetryBootstrap`.

> Aligned to code as of 2026-07-26.

## Requirements

### Requirement: ISTAvalon SHALL initialize Sentry once during process startup

`Program.Main` SHALL call `TelemetryBootstrap.Initialize(args)` before starting the Avalonia desktop lifetime. An interlocked flag ensures single initialization.

#### Scenario: Initialize before main UI is presented

- **WHEN** ISTAvalon starts with a classic desktop lifetime
- **THEN** `TelemetryBootstrap.Initialize` has already run before `MainWindow` is assigned

#### Scenario: Avoid duplicate initialization in one process

- **WHEN** `Initialize` is invoked more than once
- **THEN** subsequent calls are no-ops

### Requirement: ISTAvalon Sentry options SHALL use a privacy-conscious baseline

ISTAvalon SHALL configure Sentry with:

- Hard-coded project DSN (aligned with CLI project)
- `AutoSessionTracking = true`
- `IsGlobalModeEnabled = true`
- `SendDefaultPii = false`
- `TracesSampleRate = 0.2`
- Profiling integration enabled
- Debug builds set `Environment = "debug"`

#### Scenario: Apply baseline runtime options

- **WHEN** Sentry is initialized
- **THEN** default PII sending is disabled and traces sample rate is below 1.0

### Requirement: ISTAvalon SHALL attach non-sensitive startup and git metadata

Scope configuration SHALL:

- Set `Startup` context with `ArgumentCount` and `Flags` (flag tokens only; values after `=` and positional args omitted via `SummarizeFlags`)
- Attempt git discovery for `git.branch` and short `git.commit` only
- On missing repo or failure set `git.not_found=true`
- **Not** attach git username, email, remotes, or raw argv values

#### Scenario: Startup context omits raw arguments

- **WHEN** startup telemetry scope is configured
- **THEN** context includes argument count and flag names without positional paths or option values

#### Scenario: Git metadata without identity PII

- **WHEN** repository discovery succeeds
- **THEN** scope tags include branch and short commit only

### Requirement: ISTAvalon telemetry bootstrap SHALL fail safe for optional enrichment

Git metadata failures SHALL not abort startup. Handled exceptions set `git.not_found`.

#### Scenario: Main window still launches after git enrichment fallback

- **WHEN** optional git metadata enrichment fails
- **THEN** the main window still initializes
