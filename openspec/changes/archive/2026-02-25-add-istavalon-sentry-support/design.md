## Context

ISTAvalon currently starts Avalonia and configures Serilog, but does not explicitly initialize Sentry at startup. The existing `SentryTask` in `ISTA-Patcher` already defines the desired telemetry baseline (DSN, session tracking, global mode, PII, tracing, profiling, debug environment, and startup scope tags). This change needs ISTAvalon to adopt the same baseline with minimal divergence and clear failure behavior when local git metadata cannot be read.

Constraints:
- Keep startup sequence stable for desktop app launch.
- Avoid introducing hard failures if Sentry initialization or git inspection fails.
- Keep package/version strategy aligned with existing Sentry usage in the repository.

Stakeholders:
- Maintainers diagnosing production failures.
- Users impacted by startup/runtime crashes where telemetry improves triage.

## Goals / Non-Goals

**Goals:**
- Initialize Sentry during ISTAvalon startup, before normal app usage.
- Mirror `SentryTask` option set and semantics for telemetry parity.
- Attach startup context and git tags when available, with safe fallback when unavailable.
- Keep telemetry bootstrap isolated so it is testable and maintainable.

**Non-Goals:**
- Redesigning logging architecture beyond necessary Sentry integration points.
- Changing DSN ownership/rotation policy.
- Introducing new telemetry products or custom transport pipelines.
- Guaranteeing git metadata presence in every runtime environment.

## Decisions

1. Introduce a dedicated ISTAvalon telemetry bootstrap component.
- Decision: Add a focused startup helper/service (for example under `src/ISTAvalon/Services/`) that encapsulates `SentrySdk.Init` and scope configuration.
- Rationale: Keeps `Program.cs`/`App.axaml.cs` simple, limits coupling, and centralizes telemetry behavior for future adjustments.
- Alternatives considered:
  - Inline initialization in `Program.cs`: rejected due to reduced readability and harder testing.
  - Inline initialization in `App.axaml.cs`: rejected because telemetry should start as early as practical.

2. Align Sentry options with `SentryTask` as the source-of-truth baseline.
- Decision: Use the same effective settings (`Dsn`, `AutoSessionTracking`, `IsGlobalModeEnabled`, `SendDefaultPii`, `TracesSampleRate`, profiling integration, debug environment override).
- Rationale: Reduces cross-app behavioral drift and keeps diagnostics consistent.
- Alternatives considered:
  - Partial alignment (only DSN + session): rejected because it produces inconsistent observability quality.
  - ISTAvalon-specific tuning now: deferred until baseline parity is established.

3. Keep startup metadata enrichment best-effort and non-fatal.
- Decision: Reuse the same best-effort approach for git metadata tagging and mark missing metadata without stopping app startup.
- Rationale: Desktop startup must remain resilient; telemetry failure must not become a user-facing outage.
- Alternatives considered:
  - Fail fast when metadata unavailable: rejected due to poor UX and low value.
  - Skip metadata entirely: rejected because branch/commit context materially improves debugging.

4. Ensure package dependency consistency with existing repository strategy.
- Decision: Update `ISTAvalon.csproj` package references to the same Sentry package set/version line used by the established implementation target.
- Rationale: Avoids mixed runtime behavior and dependency fragmentation.
- Alternatives considered:
  - Independent version pinning in ISTAvalon: rejected due to maintenance overhead and drift risk.

## Risks / Trade-offs

- [Risk] Startup overhead increases slightly due to telemetry/bootstrap work. -> Mitigation: keep initialization minimal and synchronous only where required.
- [Risk] Sensitive metadata exposure if PII settings are misused. -> Mitigation: maintain existing policy, document it explicitly, and avoid adding extra user identifiers beyond current baseline.
- [Risk] SDK/version mismatch across projects causes subtle behavior differences. -> Mitigation: align package versions and verify lockfile updates during implementation.
- [Risk] Desktop environment differences can make git discovery unreliable. -> Mitigation: preserve best-effort lookup with guarded exception handling and fallback tagging.

## Migration Plan

1. Add/align Sentry package references in `src/ISTAvalon/ISTAvalon.csproj`.
2. Implement telemetry bootstrap helper and call it from ISTAvalon startup path.
3. Apply `SentryTask`-aligned option configuration and startup scope tags.
4. Validate launch behavior in Debug and non-Debug builds.
5. Rollback strategy: remove startup bootstrap invocation and Sentry package references, restoring prior startup behavior.

## Open Questions

- Should ISTAvalon share a common telemetry initializer with `ISTA-Patcher`, or keep duplication with documentation for now?
  - keep duplication
- Is `TracesSampleRate = 1` acceptable for ISTAvalon usage patterns, or should production sampling policy be parameterized later?
  - yes
- Should additional ISTAvalon-specific scope context (UI mode, command metadata) be added in a follow-up change?
  - yes
