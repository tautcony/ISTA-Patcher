## Release Note Summary

### What changed
- GUI execution now captures command output written directly to `Console.Out` and `Console.Error`.
- Captured console lines are forwarded into the same log panel stream as structured Serilog entries.
- Captured output is emitted with source markers:
  - stdout -> `[stdout]` info-level entries
  - stderr -> `[stderr]` error-level entries

### Execution lifecycle and safety
- Console redirection is scoped to command execution only.
- Original console writers are restored after execution in all paths (success, non-zero exit, and exception).
- Redirection does not leak across sequential command executions.

### UI behavior
- Log panel now shows mixed output sources (structured logs + captured console output) in one timeline.
- Existing log interactions (copy line / copy all / clear) naturally include captured console entries.
- Log message rendering now supports ANSI SGR foreground colors and reset sequences for captured console output.

### Limitations
- ANSI support currently focuses on common foreground color and reset sequences (not full terminal emulation).
- Formatting fidelity for advanced console UI output depends on how upstream libraries write to stdout/stderr.
