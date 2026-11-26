## Purpose

Describe process-wide console capture during GUI command execution, as implemented by `ConsoleCaptureScope` and `CommandExecutionService`.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: GUI SHALL capture console stdout and stderr during command execution

While `CommandExecutionService.ExecuteAsync` runs, it SHALL construct a `ConsoleCaptureScope` that replaces `Console.Out` and `Console.Error` with line-intercepting writers. Captured lines SHALL be published as `LogEntry` instances through the provided callback (implementation uses `App.LogSink.Publish`).

Line formatting:

- Standard output lines: level `Information`, message prefix `[stdout] `
- Standard error lines: level `Error`, message prefix `[stderr] `

ANSI SGR sequences in the payload are retained in the message string so the log renderer can style them; they are not stripped at capture time.

#### Scenario: Capture stdout lines during execution

- **WHEN** a command writes text to standard output while executing under the GUI capture scope
- **THEN** complete lines appear in the log stream as Information entries prefixed with `[stdout] `

#### Scenario: Capture stderr lines during execution

- **WHEN** a command writes text to standard error while executing under the GUI capture scope
- **THEN** complete lines appear in the log stream as Error entries prefixed with `[stderr] `

#### Scenario: Preserve ANSI escape information for renderer

- **WHEN** captured console output contains ANSI SGR escape sequences
- **THEN** the forwarded message payload still contains those sequences for `LogMessageHighlighter` to interpret

### Requirement: Console capture SHALL be scoped to the execution disposable and restored on dispose

`ConsoleCaptureScope` SHALL save the previous `Console.Out` / `Console.Error` writers and MUST restore them in `Dispose` after flushing interceptors. `CommandExecutionService` wraps the scope in `using`, so restoration runs on success, failure, and normal completion of the invoke path.

Capture is **process-global** for the duration of the scope. Overlapping scopes must not occur in normal GUI use because `GuiCommandExecutionGate` enforces single-flight command execution across tabs.

#### Scenario: Restore console writers after success

- **WHEN** command execution finishes successfully and the capture scope is disposed
- **THEN** the original console output/error writers are restored

#### Scenario: Restore console writers after failure

- **WHEN** command execution fails with an exception and the capture scope is disposed
- **THEN** the original console output/error writers are restored

### Requirement: GUI SHALL show mixed structured and console output in one list

Structured Serilog events (via `DelegateLogSink` subscription) and captured console lines (via `Publish`) SHALL share the same `OutputLines` collection on the active tab. Ordering is best-effort chronological based on when each source emits; there is no separate merge buffer beyond emission order and UI-thread posts.

#### Scenario: Show mixed structured and console output

- **WHEN** a command emits both Serilog log events and console text during a single tab execution subscription
- **THEN** the log panel displays both sources in the same list model
