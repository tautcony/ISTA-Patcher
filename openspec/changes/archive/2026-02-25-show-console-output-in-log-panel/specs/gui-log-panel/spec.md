## MODIFIED Requirements

### Requirement: GUI SHALL display execution logs with level-aware styling
The GUI SHALL render execution log entries in the log panel with level-aware visual styling and timestamped messages. Entries from structured logging and captured console output SHALL be displayed through the same list model so users can inspect complete execution output in one place. For captured console messages containing ANSI SGR foreground color escapes, the renderer SHALL apply corresponding foreground colors in the log panel while suppressing raw escape characters from displayed text.

#### Scenario: Render mixed log sources in one panel
- **WHEN** execution emits both structured logs and captured console output
- **THEN** the log panel displays both entry types in the same timeline view

#### Scenario: Render ANSI-colored console output
- **WHEN** a captured console line includes ANSI SGR foreground color escape sequences
- **THEN** the log panel displays the text without raw escape characters and applies the mapped foreground colors

### Requirement: GUI SHALL support log interaction utilities
The GUI SHALL allow users to clear log output, copy all output, and copy individual lines from the log panel. These utilities SHALL include captured console output entries in addition to structured log entries.

#### Scenario: Copy all includes captured console output
- **WHEN** a user runs a command that produces captured console output and selects copy-all
- **THEN** copied text includes those captured console lines with their rendered metadata
