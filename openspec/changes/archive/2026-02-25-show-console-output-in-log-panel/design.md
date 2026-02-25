## Context

`ISTAvalon` currently shows structured Serilog entries in the GUI log panel, but direct `Console` output is not captured. Some command paths (notably `CryptoCommand.LoadFileList` using table output) write to stdout and therefore become visible only in terminal mode, not in GUI mode. This creates inconsistent user feedback and makes troubleshooting harder.

The change spans command execution plumbing and log presentation in GUI:
- command execution lifetime management (`CommandExecutionService`)
- log sink integration (`DelegateLogSink` and panel feed)
- output formatting behavior for mixed streams (Serilog + console)

## Goals / Non-Goals

**Goals:**
- Capture stdout/stderr during GUI command execution and forward it to the same log panel stream.
- Preserve existing Serilog flow without regressions.
- Ensure output capture is scoped to command execution lifecycle and does not leak globally after execution.
- Handle high-volume console output safely without UI lockups.

**Non-Goals:**
- Do not redesign command business logic (`CryptoCommand`, etc.) beyond what is needed for capture compatibility.
- Do not implement full terminal emulation beyond ANSI SGR color escape handling.
- Do not change existing command exit-code semantics.

## Decisions

1. Execution-scoped console redirection wrapper
- Decision: introduce an execution-scoped stdout/stderr redirection in GUI command execution path only.
- Rationale: provides capture without altering CLI behavior or global app startup behavior.
- Alternative: global redirection at app startup.
  - Rejected due to broader side effects and higher risk of unrelated output contamination.

2. Convert captured console lines into log-panel entries
- Decision: map captured stdout lines to info-level log entries and stderr lines to warning/error-level entries in log panel model.
- Rationale: keeps one unified output pane and consistent timestamped records.
- Alternative: separate "Console" pane.
  - Rejected to avoid UX fragmentation and larger UI changes.

3. Keep Serilog and console streams additive (no suppression)
- Decision: do not suppress existing Serilog logs; append captured console messages as additional entries.
- Rationale: preserves current observability while adding missing source.
- Alternative: disable some Serilog lines to avoid possible duplication.
  - Rejected because duplication risk is command-specific and easier to handle with formatting guidance than global suppression.

4. Line-buffered forwarding with UI thread dispatch
- Decision: forward captured text line-by-line and dispatch to UI thread through existing pattern.
- Rationale: predictable rendering and lower contention versus per-character updates.
- Alternative: batch flush on completion.
  - Rejected because it loses real-time feedback.

5. Deterministic cleanup and restoration
- Decision: always restore original Console writers in `finally` blocks, even on exceptions/cancel.
- Rationale: prevents cross-command leakage and protects later operations.

6. ANSI color escape handling in log rendering
- Decision: support ANSI SGR foreground color escapes (`30-37`, `90-97`) and reset (`0`, `39`) in log panel rendering.
- Rationale: preserves readable color semantics for captured console output with minimal parser complexity.
- Alternative: strip ANSI escapes and display plain text only.
  - Rejected because useful severity/context coloring is lost for command output.

## Risks / Trade-offs

- [Duplicate messages when both console and Serilog emit similar content] -> Keep source-tag formatting and evaluate targeted deduplication only if needed.
- [UI performance degradation with high-frequency console output] -> Use line buffering and bounded dispatch strategy.
- [Thread-safety issues in writer callbacks] -> Ensure synchronized write/flush implementation and main-thread marshaling for UI collection updates.
- [Unsupported ANSI variants (background/24-bit/style codes) may render as default color] -> Support core foreground codes now and safely ignore unsupported codes.

## Migration Plan

1. Add a console-capture helper (scoped disposable) in `ISTAvalon` execution layer.
2. Integrate helper into command execution path so capture starts before command invocation and ends after completion.
3. Route captured stdout/stderr lines into existing log panel feed with timestamps and level mapping.
4. Add ANSI-aware message rendering in log highlighter.
5. Add tests for capture lifecycle, mixed output ordering expectations, ANSI rendering, and restoration behavior.
6. Validate with `crypto` command path that emits table output (`LoadFileList`).

## Open Questions

- Should captured console lines include a `[stdout]` / `[stderr]` prefix in the message text by default?
- For multiline/table output, should blank lines be preserved exactly or normalized?
- Should we extend ANSI support to background colors, bold/italic styles, and 24-bit color sequences?
