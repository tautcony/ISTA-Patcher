## ADDED Requirements

### Requirement: ISTAvalon SHALL initialize Sentry during desktop app startup
ISTAvalon SHALL initialize Sentry before the main window is shown so startup and early runtime failures can be captured. The initialization path MUST execute once per application process.

#### Scenario: Initialize before main UI is presented
- **WHEN** ISTAvalon starts with a classic desktop lifetime
- **THEN** Sentry is initialized before `MainWindow` is assigned and shown

#### Scenario: Avoid duplicate initialization in one process
- **WHEN** startup logic runs for a single process instance
- **THEN** Sentry initialization is performed exactly once

### Requirement: ISTAvalon Sentry options SHALL match the SentryTask baseline
ISTAvalon SHALL configure Sentry with the same effective baseline used by `SentryTask`: DSN, auto session tracking, global mode, default PII sending, traces sample rate, profiling integration, and debug-only environment override. ISTAvalon package versions for Sentry dependencies MUST align with the version strategy used for the SentryTask baseline to prevent telemetry behavior drift.

#### Scenario: Apply baseline runtime options
- **WHEN** Sentry is initialized in ISTAvalon
- **THEN** the runtime options include the full SentryTask-equivalent baseline option set

#### Scenario: Debug build sets debug environment
- **WHEN** ISTAvalon runs a Debug build
- **THEN** the Sentry environment is explicitly tagged as `debug`

#### Scenario: Dependency versions remain aligned
- **WHEN** Sentry dependencies are declared for ISTAvalon
- **THEN** their versions align with the repository baseline used by `SentryTask`

### Requirement: ISTAvalon SHALL attach startup and repository context to Sentry scope
ISTAvalon SHALL attach startup context to Sentry scope and SHALL attempt to enrich scope tags with git metadata (username, email, branch, commit, remotes) when repository information is discoverable.

#### Scenario: Startup context is attached
- **WHEN** startup telemetry scope is configured
- **THEN** scope context contains startup parameters or equivalent startup metadata payload

#### Scenario: Git metadata discovered successfully
- **WHEN** repository discovery and metadata reads succeed
- **THEN** scope tags include git identity, branch, commit, and remote URL information

### Requirement: ISTAvalon telemetry bootstrap SHALL fail safe
Failures while gathering optional metadata or configuring telemetry SHALL NOT block application startup. ISTAvalon MUST continue launching and SHOULD record a fallback indicator when git metadata is unavailable.

#### Scenario: Git metadata discovery fails
- **WHEN** repository discovery or metadata retrieval throws a handled exception
- **THEN** ISTAvalon startup continues and telemetry scope records a fallback marker indicating git metadata was unavailable

#### Scenario: Main window still launches after telemetry fallback
- **WHEN** optional telemetry enrichment fails during startup
- **THEN** the main window still initializes and application startup completes
