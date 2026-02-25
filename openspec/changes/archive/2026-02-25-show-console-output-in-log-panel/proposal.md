## Why

Currently, command output written directly to `Console` is not captured by the GUI log panel, so users miss important runtime information. This creates an observability gap, especially for commands like `crypto` (`LoadFileList`) that print tabular or diagnostic output directly.

## What Changes

- Add a console-output capture mechanism so text written to standard output/error during command execution is also forwarded to the GUI log panel.
- Ensure command flows that use `Console`/`Spectre.Console` output (for example, `CryptoCommand.LoadFileList`) are visible in the same log stream as structured logger entries.
- Add ANSI color escape rendering support in the log panel so captured console output can preserve color semantics.
- Keep existing log behavior intact while preventing duplicated or garbled messages.
- Add tests and verification coverage for mixed output sources (Serilog + console writes).

## Capabilities

### New Capabilities
- `gui-console-output-capture`: Define how console stdout/stderr is captured, formatted, and displayed in the GUI log panel during command execution.

### Modified Capabilities
- `gui-command-invocation`: Extend execution-output requirements so invocation reflects both structured log events and raw console output in real time.
- `gui-log-panel`: Update log panel behavior requirements to include presentation rules for forwarded console output.
  including ANSI color escape rendering.

## Impact

- Affected code includes command execution plumbing in `ISTAvalon` (log sink/execution service) and potentially output pathways used by command handlers.
- Commands relying on console output (e.g., crypto file list rendering) gain visibility in GUI diagnostics without changing their core business logic.
- Test coverage needs updates for output capture lifecycle, concurrency safety, formatting consistency, and ANSI color rendering behavior.
