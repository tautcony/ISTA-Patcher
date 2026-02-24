## ADDED Requirements

### Requirement: Log panel SHALL be positioned as a right-side column
The log panel SHALL render to the right of the parameter form within each command tab, occupying a resizable column separated by a draggable splitter.

#### Scenario: Default layout shows log panel on the right
- **WHEN** a command tab is displayed
- **THEN** the parameter form occupies the left column and the log panel occupies the right column with a draggable splitter between them

#### Scenario: User resizes log panel via splitter
- **WHEN** the user drags the splitter
- **THEN** the relative widths of the parameter form and log panel adjust accordingly

### Requirement: Log panel SHALL be collapsible
The log panel SHALL provide a toggle control that collapses the panel to zero width and expands it back to its previous width.

#### Scenario: Collapse the log panel
- **WHEN** the user activates the collapse toggle while the log panel is expanded
- **THEN** the log panel collapses to zero width and the parameter form fills the available space

#### Scenario: Expand the log panel
- **WHEN** the user activates the collapse toggle while the log panel is collapsed
- **THEN** the log panel restores to its previous width

### Requirement: Each log entry SHALL display a timestamp
Every log entry rendered in the panel SHALL display a timestamp formatted as `HH:mm:ss.fff` preceding the log message.

#### Scenario: Log entry shows millisecond-precision timestamp
- **WHEN** a log entry is added to the output
- **THEN** the entry displays with a timestamp in `HH:mm:ss.fff` format reflecting the time the log event was emitted

### Requirement: Log entries SHALL be highlighted by severity level
Each log entry SHALL have its foreground color determined by its log severity level, using colors sourced from the palette configuration.

#### Scenario: Error-level entry is visually distinct
- **WHEN** a log entry with Error level is rendered
- **THEN** the entry foreground uses the palette's Error color

#### Scenario: Warning-level entry is visually distinct
- **WHEN** a log entry with Warning level is rendered
- **THEN** the entry foreground uses the palette's Warning color

#### Scenario: Verbose/Debug-level entry appears subdued
- **WHEN** a log entry with Verbose or Debug level is rendered
- **THEN** the entry foreground uses the palette's subdued Verbose/Debug color

### Requirement: Log message content SHALL have inline syntax highlighting
Within each log message body, quoted strings and numeric literals SHALL be highlighted with distinct colors sourced from the palette configuration.

#### Scenario: Quoted string is highlighted
- **WHEN** a log message contains text enclosed in double or single quotes
- **THEN** the quoted segment is rendered with the palette's String color

#### Scenario: Numeric literal is highlighted
- **WHEN** a log message contains a contiguous numeric value (integer or decimal)
- **THEN** the numeric segment is rendered with the palette's Number color

#### Scenario: Plain text retains level-based color
- **WHEN** a log message segment is neither a quoted string nor a numeric literal
- **THEN** the segment is rendered with the log-level foreground color

### Requirement: A `LogPanelPalette` class SHALL centralise all log panel colors
The system SHALL provide a `LogPanelPalette` class that exposes brush properties for every color used in the log panel: per-level foregrounds (Verbose, Debug, Information, Warning, Error, Fatal), token foregrounds (String, Number), and panel chrome (Timestamp, Background, Header). All consumers SHALL read colors from this class.

#### Scenario: Changing a palette brush affects new log entries
- **WHEN** a `LogPanelPalette` brush property value is changed
- **THEN** newly rendered log entries use the updated brush

#### Scenario: Default palette provides sensible colors
- **WHEN** the application starts with no palette customisation
- **THEN** all palette brush properties have non-null default values suitable for a dark background theme

### Requirement: Log entries SHALL support text selection and copy
Users SHALL be able to select text within individual log entries and copy it to the system clipboard.

#### Scenario: Select and copy text from a single log entry
- **WHEN** the user selects text within a log entry and invokes copy (Ctrl+C / Cmd+C)
- **THEN** the selected text is placed on the system clipboard

#### Scenario: Copy all log output via context menu
- **WHEN** the user invokes "Copy All" from the log panel context menu
- **THEN** the full text of all visible log entries (with timestamps) is placed on the system clipboard

### Requirement: Log panel SHALL provide a Clear control
The log panel header SHALL include a Clear button that removes all accumulated log entries from the output.

#### Scenario: Clear button removes all entries
- **WHEN** the user activates the Clear button
- **THEN** all log entries are removed from the output and the status text resets

#### Scenario: Clear does not affect command execution state
- **WHEN** the user activates Clear while a command is executing
- **THEN** the accumulated entries are removed but the running command continues and new entries keep appearing
