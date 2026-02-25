## ADDED Requirements

### Requirement: GUI SHALL capture console stdout and stderr during command execution
The GUI command execution pipeline SHALL capture text written to `Console.Out` and `Console.Error` while a command is executing, and SHALL forward captured output to the log panel stream in near real time.

#### Scenario: Capture stdout lines during execution
- **WHEN** a command writes text to standard output while executing in GUI mode
- **THEN** the written lines appear in the log panel before command completion

#### Scenario: Capture stderr lines during execution
- **WHEN** a command writes text to standard error while executing in GUI mode
- **THEN** the written lines appear in the log panel as error-oriented entries

#### Scenario: Preserve ANSI escape information for renderer
- **WHEN** captured console output contains ANSI SGR escape sequences
- **THEN** the forwarded message payload retains enough information for the log renderer to apply ANSI color styling

### Requirement: Console capture SHALL be execution-scoped and restored safely
The system SHALL scope console redirection to the active command execution window and MUST restore original console writers after execution completes, including failure paths.

#### Scenario: Restore console writers after success
- **WHEN** command execution finishes successfully
- **THEN** the original console output/error writers are restored

#### Scenario: Restore console writers after failure
- **WHEN** command execution fails with an exception
- **THEN** the original console output/error writers are restored

### Requirement: GUI SHALL preserve readable ordering of mixed output sources
The GUI SHALL preserve a readable temporal sequence when displaying mixed output from structured logs and captured console lines, so users can follow execution flow.

#### Scenario: Show mixed structured and console output
- **WHEN** a command emits both Serilog log events and console text
- **THEN** the log panel displays both output sources in a coherent time-ordered sequence
