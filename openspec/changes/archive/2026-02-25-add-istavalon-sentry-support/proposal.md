## Why

ISTAvalon currently lacks explicit Sentry initialization behavior aligned with the main application's startup telemetry setup. Adding this now improves crash visibility and startup diagnostics parity across apps while reducing investigation time for production issues.

## What Changes

- Add Sentry support to ISTAvalon startup flow with configuration aligned to `SentryTask` (including package version alignment and equivalent runtime options).
- Ensure ISTAvalon initializes Sentry early in app startup and attaches useful startup context for diagnostics.
- Define expected behavior for environment tagging and safe fallback when git metadata cannot be collected.

## Capabilities

### New Capabilities
- `istavalon-sentry-telemetry`: Define requirements for initializing Sentry in ISTAvalon with `SentryTask`-aligned options (DSN, session tracking, global mode, PII, traces sample rate, profiling integration, debug environment tagging) and startup context capture.

### Modified Capabilities
- None.

## Impact

- Affected code:
  - `src/ISTAvalon/Program.cs`
  - `src/ISTAvalon/App.axaml.cs`
  - `src/ISTAvalon/ISTAvalon.csproj`
  - Potential new startup/telemetry helper under `src/ISTAvalon/Services/`
- Dependencies:
  - Sentry-related packages for ISTAvalon will need to match the version strategy used by `SentryTask`.
- Systems:
  - Runtime telemetry pipeline and startup logging behavior for ISTAvalon.
