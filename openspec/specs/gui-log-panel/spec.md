## Purpose

Describe the per-tab right-side log panel UI, styling, and interactions as implemented in ISTAvalon.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: Log panel SHALL be positioned as a right-side column

The log panel SHALL render to the right of the parameter form within each command tab. A `GridSplitter` separates the columns when the panel is expanded. The log column width is driven by `IsLogPanelExpanded` via `BoolToGridLengthConverter` (expanded: star-sized column; collapsed: zero width).

#### Scenario: Default layout shows log panel on the right

- **WHEN** a command tab is displayed with the log panel expanded (default `IsLogPanelExpanded = true`)
- **THEN** the parameter form occupies the left column and the log panel occupies the right column with a splitter between them

#### Scenario: User resizes log panel via splitter

- **WHEN** the user drags the splitter while the panel is expanded
- **THEN** the relative widths of the parameter form and log panel adjust

### Requirement: Log panel SHALL be collapsible

The log panel SHALL toggle via `ToggleLogPanelCommand` / `IsLogPanelExpanded`. Collapse hides the log border and splitter and shows an expand control; expand shows the log column again at the converter's expanded grid length (not a remembered custom pixel width).

#### Scenario: Collapse the log panel

- **WHEN** the user activates collapse while expanded
- **THEN** the log panel is hidden and the parameter form uses the available space

#### Scenario: Expand the log panel

- **WHEN** the user activates expand while collapsed
- **THEN** the log panel becomes visible again in the right column

### Requirement: Each log entry SHALL display a timestamp

Every log entry rendered in the panel SHALL display a timestamp formatted as `HH:mm:ss.fff` preceding the log message (from `LogEntry.Timestamp`).

#### Scenario: Log entry shows millisecond-precision timestamp

- **WHEN** a log entry is added to the output
- **THEN** the entry displays with a timestamp in `HH:mm:ss.fff` format

### Requirement: Log entries SHALL be highlighted by severity level

Each log entry's default foreground SHALL come from `LogPanelPalette` brushes for Verbose, Debug, Information, Warning, Error, and Fatal.

#### Scenario: Error-level entry is visually distinct

- **WHEN** a log entry with Error level is rendered
- **THEN** the entry foreground uses `LogPanelPalette.ErrorBrush`

#### Scenario: Warning-level entry is visually distinct

- **WHEN** a log entry with Warning level is rendered
- **THEN** the entry foreground uses `LogPanelPalette.WarningBrush`

#### Scenario: Verbose/Debug-level entry appears subdued

- **WHEN** a log entry with Verbose or Debug level is rendered
- **THEN** the entry foreground uses the corresponding subdued palette brush

### Requirement: Log message content SHALL have inline syntax highlighting

Within each log message body, quoted strings and numeric literals SHALL be highlighted using `LogPanelPalette.StringBrush` and `NumberBrush`. Plain segments keep the level brush. ANSI SGR-aware highlighting applies when escape sequences are present.

#### Scenario: Quoted string is highlighted

- **WHEN** a log message contains text enclosed in double or single quotes
- **THEN** the quoted segment uses the string brush

#### Scenario: Numeric literal is highlighted

- **WHEN** a log message contains a contiguous numeric value (integer or decimal)
- **THEN** the numeric segment uses the number brush

#### Scenario: Plain text retains level-based color

- **WHEN** a log message segment is neither quoted nor numeric (and not ANSI-styled)
- **THEN** the segment uses the log-level foreground brush

### Requirement: GUI SHALL display execution logs with level-aware styling

Structured logging entries and captured console output SHALL share the same `OutputLines` list model. For messages containing ANSI SGR foreground escapes, the renderer SHALL apply mapped colors and suppress raw escape characters in displayed runs.

#### Scenario: Render mixed log sources in one panel

- **WHEN** execution emits both structured logs and captured console output into the tab list
- **THEN** both appear in the same timeline list

#### Scenario: Render ANSI-colored console output

- **WHEN** a captured console line includes ANSI SGR foreground color escape sequences
- **THEN** the log panel displays text without raw escapes and applies mapped foreground colors

### Requirement: A LogPanelPalette type SHALL centralise log panel colors

The system SHALL provide `ISTAvalon.Models.LogPanelPalette` as a **static** class exposing settable brush properties for level foregrounds, token colors, panel chrome, and ANSI standard/bright colors. Consumers SHALL read colors from this type.

#### Scenario: Changing a palette brush affects new log entries

- **WHEN** a `LogPanelPalette` brush property value is changed
- **THEN** newly rendered highlighted runs use the updated brush

#### Scenario: Default palette provides non-null brushes

- **WHEN** the application starts with no palette customisation
- **THEN** palette brush properties have non-null default values

#### Scenario: ANSI palette brushes cover standard and bright colors

- **WHEN** ANSI SGR foreground codes specify standard (30–37) or bright (90–97) colors
- **THEN** corresponding `LogPanelPalette` brushes exist for mapping

### Requirement: A HighlightedLogTextBlock control SHALL render syntax-highlighted log entries

The system SHALL provide `HighlightedLogTextBlock` extending `SelectableTextBlock`, binding a `LogEntry` and repopulating inlines via `LogMessageHighlighter.Highlight`.

#### Scenario: Control binds to LogEntry and renders highlighted runs

- **WHEN** a `HighlightedLogTextBlock` receives a new `LogEntry`
- **THEN** its inlines are replaced with highlighted runs

#### Scenario: Control supports text selection

- **WHEN** the user selects text within a `HighlightedLogTextBlock`
- **THEN** the selection can be copied with the platform selection mechanism

### Requirement: A LogMessageHighlighter class SHALL tokenise log messages

`LogMessageHighlighter` SHALL convert a message string and log level into ordered `Run` elements, tokenising quotes/numbers and switching to ANSI-aware processing when `\u001b` sequences are present.

#### Scenario: Tokenise quoted strings and numbers in plain messages

- **WHEN** `Highlight()` is called with `"quoted"` or numeric literals
- **THEN** returned runs separate those tokens with string/number brushes

#### Scenario: ANSI-aware processing for console output

- **WHEN** `Highlight()` is called with ANSI SGR sequences
- **THEN** returned runs apply mapped ANSI colors and omit raw escapes

#### Scenario: Non-ANSI, non-token text uses level color

- **WHEN** a plain segment is neither quoted nor numeric
- **THEN** it uses the level brush

### Requirement: Log entries SHALL support text selection and copy

Users SHALL be able to select text within individual entries. Copy All (`CopyAllCommand`) SHALL place all current `OutputLines` on the clipboard as lines of the form  
`{HH:mm:ss.fff} [{Level}] {Message}` joined by `Environment.NewLine`.

#### Scenario: Select and copy text from a single log entry

- **WHEN** the user selects text within a log entry and invokes copy
- **THEN** the selected text is available to the system clipboard

#### Scenario: Copy all log output

- **WHEN** the user invokes Copy All from the header button or context menu
- **THEN** all current log entries (structured and console-captured) are copied with timestamps and levels

### Requirement: Log panel SHALL provide a Clear control

Clear (`ClearOutputCommand`) SHALL clear `OutputLines` and set `StatusText` to `Ready`. Clearing does not cancel a running command; new entries may continue to appear if still subscribed.

#### Scenario: Clear button removes all entries

- **WHEN** the user activates Clear
- **THEN** all log entries are removed and status text becomes `Ready`

#### Scenario: Clear does not stop command execution

- **WHEN** the user activates Clear while a command is executing
- **THEN** entries are removed but the running command continues and new entries keep appearing
