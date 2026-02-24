## Context

ISTAvalon currently renders command execution output as a plain `ObservableCollection<string>` inside a bottom-docked `ItemsControl`. The `DelegateLogSink` forwards only the rendered message string, discarding timestamp, level, and structured properties. The log panel has no interactive features — no selection, no copy, no clear, no collapse.

The proposal calls for moving the log panel to the right, enriching each log entry with metadata (timestamp + level), adding multi-layer highlighting (level + inline syntax), making the panel collapsible, supporting copy/clear, and centralising all colors in a palette class.

## Goals / Non-Goals

**Goals:**
- Provide a structured log entry model that carries timestamp, level, and rendered message.
- Move the log panel to a right-side column and make it collapsible.
- Highlight log lines by severity level using configurable colors.
- Perform lightweight inline syntax highlighting on log message bodies (quoted strings, numeric literals).
- Centralise all log-panel colors in a single `LogPanelPalette` class for easy theming.
- Support text selection and clipboard copy of log content.
- Provide a Clear button to reset accumulated output.

**Non-Goals:**
- Full regex-based log parsing or structured-logging property extraction.
- Persistent log storage or export-to-file from the GUI.
- User-editable palette at runtime (palette is code-configurable, not a settings UI).
- Rich text editor features (find-in-log, filtering by level).

## Decisions

### Decision 1: Structured log entry model (`LogEntry`)

Introduce a `LogEntry` record with `DateTimeOffset Timestamp`, `LogEventLevel Level`, and `string Message`. `DelegateLogSink.Emit` will forward a `LogEntry` instead of a plain `string`. `CommandTabViewModel.OutputLines` changes from `ObservableCollection<string>` to `ObservableCollection<LogEntry>`.

**Why:** Carrying metadata alongside the message is the prerequisite for timestamp display and level-based coloring. A lightweight record avoids unnecessary allocations.

**Alternatives considered:**
- Tuple `(DateTime, int, string)` — less readable, no type safety on level.
- Keep `string` and parse level back out of rendered text — fragile and wasteful.

### Decision 2: Right-side layout with collapsible splitter

Replace the current `DockPanel` layout inside the tab `ContentTemplate` with a two-column `Grid`. The left column holds the parameter `ScrollViewer` + execute bar; the right column holds the log panel. A `GridSplitter` between them allows resizing. A toggle button in the log panel header collapses the right column (`Width = 0` via binding) and inverts icon direction.

**Why:** A `Grid` with `GridSplitter` is the idiomatic Avalonia approach for resizable side-by-side panels. Collapse via column width binding avoids complex visual-state management.

**Alternatives considered:**
- `SplitView` — designed for navigation panes, not content panels.
- Keep `DockPanel` with `Dock="Right"` — no built-in resize affordance.

### Decision 3: Log-level row coloring via `IValueConverter`

Create `LogLevelToBrushConverter` that maps `LogEventLevel` → `IBrush` using colors sourced from `LogPanelPalette`. The converter is registered as a static resource and used in the log line `DataTemplate` to set `Foreground`.

**Why:** A converter keeps the XAML declarative and cleanly separates color logic from the view.

**Alternatives considered:**
- DataTrigger / style selectors — more verbose, harder to centralise palette lookup.
- ViewModel property `LevelBrush` — couples presentation concern into VM.

### Decision 4: Inline syntax highlighting via `InlineCollection` builder

Create a `LogMessageHighlighter` utility class that takes a raw log message string and returns an `IEnumerable<Inline>` (`Run` elements). It performs a single-pass scan using a simple state machine / regex to identify:
- **Quoted strings** (text between `"` or `'`) → colored with `Palette.StringColor`
- **Numeric literals** (contiguous digits, optional decimal point) → colored with `Palette.NumberColor`
- **Plain text** → colored with the log-level foreground

The resulting `Inline` collection is set on a `SelectableTextBlock.Inlines` in the log line template via an attached behavior or a custom control.

**Why:** Avalonia's `TextBlock.Inlines` / `SelectableTextBlock.Inlines` supports mixed-color `Run` elements natively. A utility class keeps the tokenisation testable and separate from the view layer.

**Alternatives considered:**
- `FormattedText` / `DrawingContext` custom render — much more complex, no built-in selection.
- RichTextBox — heavyweight, unnecessary for read-only highlighting.

### Decision 5: `LogPanelPalette` configuration class

A static class `LogPanelPalette` in `ISTAvalon.Models` that exposes `IBrush` properties for:
- Per-level foreground: `VerboseBrush`, `DebugBrush`, `InformationBrush`, `WarningBrush`, `ErrorBrush`, `FatalBrush`
- Token foreground: `StringBrush`, `NumberBrush`
- Panel chrome: `TimestampBrush`, `BackgroundBrush`, `HeaderBrush`

All properties initialise to sensible defaults (e.g., dark-theme–friendly colors). Consumers read from `LogPanelPalette` at render time, so changing a brush property takes effect on new lines immediately.

**Why:** A single palette class eliminates scattered color literals, makes it trivial to re-skin, and is the foundation for future theme-aware palettes.

**Alternatives considered:**
- Avalonia `ResourceDictionary` theme resources — good long-term, but heavier to set up for an initial iteration; palette class can delegate to resources later.
- Per-converter hardcoded colors — exactly the anti-pattern the user wants to avoid.

### Decision 6: Copy support via `SelectableTextBlock` + context menu

Use `SelectableTextBlock` (Avalonia 11.x built-in) for each log line instead of `TextBlock`. Add a `ContextMenu` with "Copy" and "Copy All" items on the log panel `ListBox` (or `ItemsRepeater`). "Copy" copies the selected text from a single `SelectableTextBlock`; "Copy All" joins all `OutputLines` messages with newlines and copies to clipboard.

**Why:** `SelectableTextBlock` provides native OS text selection and copy for free; the context menu extends this to bulk copy.

**Alternatives considered:**
- Single large `TextBox` (readonly) — simpler selection, but loses per-line metadata and inline span coloring.
- Custom selection overlay — excessive effort.

### Decision 7: Clear button and panel header

The log panel header is a small `DockPanel` with: a "Log" label, a Clear (`🗑`) button bound to a `ClearOutputCommand` on `CommandTabViewModel`, and the collapse toggle button. `ClearOutputCommand` simply calls `OutputLines.Clear()` and resets `StatusText`.

**Why:** Minimal UI surface for maximum utility. `RelayCommand` keeps it consistent with existing command pattern.

## Risks / Trade-offs

- **Inline highlighting performance on large output** → Mitigation: cap `OutputLines` at a configurable max (e.g., 5000 entries), dropping oldest; the tokeniser is a single linear pass so per-line cost is O(n) in message length.
- **`SelectableTextBlock` with `Inlines` binding** → Avalonia may not support direct `Inlines` binding in all versions. Mitigation: use an attached behavior that manually sets `Inlines` from the bound collection, falling back to a plain `Text` binding if `Inlines` is unavailable.
- **Palette not yet theme-aware** → The static palette won't auto-switch between light/dark themes. Mitigation: acceptable for v1; document that a future iteration can bridge `LogPanelPalette` to Avalonia theme resources.

## Open Questions

- Should the log panel default to collapsed or expanded on startup?
- Should the max output line cap be exposed as a user setting or remain a compile-time constant?
