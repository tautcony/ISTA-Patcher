## 1. Execution Pipeline and Console Capture

- [x] 1.1 Identify current GUI command execution lifecycle entry/exit points in `CommandExecutionService` and related log-sink wiring.
- [x] 1.2 Implement an execution-scoped console capture helper that redirects `Console.Out` and `Console.Error` during command execution.
- [x] 1.3 Ensure capture helper restores original console writers in all completion paths (success, command failure, exception).
- [x] 1.4 Implement line-buffered forwarding of captured stdout/stderr content to the GUI log-entry pipeline.

## 2. Log Panel Integration

- [x] 2.1 Integrate captured console output into the existing log panel data flow used by `CommandTabViewModel`.
- [x] 2.2 Apply consistent entry metadata (timestamp + source/level mapping) for captured stdout and stderr messages.
- [x] 2.3 Verify mixed output (Serilog + console) is rendered in one timeline and remains readable under normal command execution.
- [x] 2.4 Ensure copy-all and copy-line actions include captured console output entries.
- [x] 2.5 Add ANSI SGR foreground color rendering support in log message highlighting for captured console lines.

## 3. Command Invocation Behavior

- [x] 3.1 Update command invocation handling so console capture starts before command invocation and ends after command completion.
- [x] 3.2 Validate that required-parameter blocking behavior is unchanged when capture is enabled.
- [x] 3.3 Validate that existing status messages and execution result semantics are unchanged.

## 4. Tests and Regression Coverage

- [x] 4.1 Add/adjust tests for execution-scoped console capture lifecycle and writer restoration.
- [x] 4.2 Add/adjust tests for stdout/stderr capture forwarding into log entries.
- [x] 4.3 Add/adjust tests for mixed Serilog + console output ordering/readability assumptions.
- [x] 4.4 Add/adjust tests ensuring log panel interaction utilities include captured console output.
- [x] 4.5 Add a focused validation case for a command path that writes direct console output (e.g., crypto list/table output path).
- [x] 4.6 Add/adjust tests for ANSI escape parsing and color reset behavior in log rendering.

## 5. Validation and Rollout

- [x] 5.1 Run targeted test suites for `ISTAvalon` execution/logging behavior and affected command paths.
- [x] 5.2 Perform manual GUI verification: execute command that writes to console and confirm output appears in log panel in real time.
- [x] 5.3 Confirm no global console redirection leakage by running multiple commands sequentially.
- [x] 5.4 Prepare a concise change note describing new console-output-to-log-panel behavior and any formatting limits.
