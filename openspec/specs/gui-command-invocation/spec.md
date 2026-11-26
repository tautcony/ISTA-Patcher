## Purpose

Describe how the GUI invokes discovered CLI commands from form state, surfaces logs and outcomes, and validates required parameters.

> Aligned to code as of 2026-07-26.

## Requirements

### Requirement: GUI SHALL execute commands from current form state

The GUI SHALL set command/parent properties from parameter editors and invoke public instance `RunAsync`. Inherited and parent-option parameters from discovery SHALL be included.

During execution the active tab SHALL subscribe to `App.LogSink` and capture console via `ConsoleCaptureScope`.

Before execution, required parameters (`IsRequired && !HasValue`) SHALL be validated; on failure status is  
`⚠ Required: {display names}`.

Cancellation uses a per-run `CancellationTokenSource` assigned to the command when a writable `CancellationToken` property exists.

#### Scenario: Execute command with inherited parameters provided

- **WHEN** a user triggers execution with all required parameters filled
- **THEN** the GUI creates instances, sets properties, and invokes `RunAsync`

#### Scenario: Block execution when required parameters are missing

- **WHEN** required parameters lack values
- **THEN** the GUI does not invoke the command and lists missing display names

### Requirement: GUI command execution SHALL be globally single-flight

`GuiCommandExecutionGate` SHALL ensure at most one GUI command run process-wide. A second tab (or re-entry) that cannot enter the gate SHALL set status to  
`⚠ Another command is already running.` and MUST NOT subscribe to the log sink or capture console.

Execute CanExecute is false while this tab's `IsExecuting` is true **or** the global gate is busy.

#### Scenario: Concurrent tab execution is rejected

- **WHEN** one tab is already executing
- **THEN** another tab cannot start execution until the gate is released

#### Scenario: Same tab cannot double-start

- **WHEN** a tab is already executing
- **THEN** its Execute command remains disabled until completion

### Requirement: GUI SHALL present execution progress and outcome from exit codes and exceptions

Structured logs and captured console lines appear in the tab `OutputLines` list.

Outcome rules:

- `RunAsync` returning `Task<int>` → use the integer exit code
- `RunAsync` returning non-generic `Task` → treated as exit code `0`
- Exit code `0` → success status message
- Non-zero → exit-code warning status
- `OperationCanceledException` → cancelled status + warning log entry
- Other exceptions → failure status + error log entry

Primary GUI commands (`patch`, `ilean`, `crypto`, and hidden `cerebrumancy`) SHALL expose `Task<int> RunAsync` that returns the result of their `Execute` methods.

#### Scenario: Show successful command outcome for zero exit code

- **WHEN** invocation completes with exit code 0
- **THEN** the status area shows the success message

#### Scenario: Show non-zero exit code outcome

- **WHEN** invocation completes with non-zero `Task<int>`
- **THEN** the status area shows the exit-code warning

#### Scenario: Stream structured log entries during execution

- **WHEN** a command produces Serilog output while the tab is subscribed
- **THEN** entries appear in the log panel with timestamp, level, and message
