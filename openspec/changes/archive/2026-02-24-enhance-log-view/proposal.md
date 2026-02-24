## Why

The current ISTAvalon log output panel is a plain, bottom-docked `ItemsControl` that only appends raw text lines. Users cannot tell when each log entry occurred, cannot select or copy individual log lines, have no way to clear accumulated output between runs, and lack visual cues to distinguish severity levels. For any non-trivial command execution, this makes diagnosis difficult and the log panel hard to use.

## What Changes

- **Move log panel to the right side** of the command tab layout (instead of bottom-docked below parameters) to provide a taller, dedicated log viewing area alongside the parameter form.
- **Add timestamps** to every log line displayed in the output panel, formatted as `HH:mm:ss.fff`.
- **Add log-level highlighting** — visually distinguish log entries by severity (e.g., Error in red, Warning in amber, Info in default, Debug/Verbose in subdued color).
- **Add content-level syntax highlighting** — beyond log levels, highlight inline strings (quoted text), numbers, and other token types within log message bodies using configurable colors.
- **Introduce a `LogPanelPalette` configuration class** — a dedicated class that centralises all color definitions (log-level colors, token-type colors, background, timestamp color, etc.) so the palette can be adjusted in one place.
- **Add text selection and copy support** — allow users to select one or more log lines and copy them to clipboard via context menu or keyboard shortcut.
- **Add a Clear button** in the log panel header to manually clear accumulated output without re-executing.
- **Make the log panel collapsible** — allow users to collapse/expand the log panel to maximise parameter editing space when the log is not needed.

## Capabilities

### New Capabilities
- `gui-log-panel`: Covers log panel layout (including collapsibility), timestamp rendering, severity-based highlighting, content-level syntax highlighting (strings, numbers, tokens), text selection/copy, clear functionality, and the `LogPanelPalette` configuration class.

### Modified Capabilities
- `gui-command-invocation`: The execution outcome display now routes structured log entries (with timestamp and level) to the enhanced log panel instead of plain strings.

## Impact

- **Views**: [MainWindow.axaml](src/ISTAvalon/Views/MainWindow.axaml) layout restructured — log panel moves from bottom `DockPanel.Dock="Bottom"` to a right-side `Grid` column.
- **ViewModels**: `CommandTabViewModel` — `OutputLines` changes from `ObservableCollection<string>` to a structured log-entry collection carrying timestamp and level metadata.
- **Services**: `DelegateLogSink` — must forward `LogEventLevel` and timestamp alongside the rendered message.
- **No new external dependencies** — Avalonia's built-in `SelectableTextBlock`, context menus, and theme brushes are sufficient.
