## 1. Models & Palette

- [x] 1.1 Create `LogEntry` record in `Models/LogEntry.cs` with `DateTimeOffset Timestamp`, `LogEventLevel Level`, and `string Message`
- [x] 1.2 Create `LogPanelPalette` static class in `Models/LogPanelPalette.cs` with `IBrush` properties for per-level foregrounds (Verbose, Debug, Information, Warning, Error, Fatal), token foregrounds (String, Number), and panel chrome (Timestamp, Background, Header) initialised to dark-theme defaults

## 2. Service Layer Updates

- [x] 2.1 Update `DelegateLogSink` to emit `LogEntry` instead of plain `string` — capture `logEvent.Timestamp`, `logEvent.Level`, and `logEvent.RenderMessage()`
- [x] 2.2 Update `DelegateLogSink.Subscribe` signature from `Action<string>` to `Action<LogEntry>`

## 3. ViewModel Updates

- [x] 3.1 Change `CommandTabViewModel.OutputLines` from `ObservableCollection<string>` to `ObservableCollection<LogEntry>`
- [x] 3.2 Update log subscription in `ExecuteCommandAsync` to receive `LogEntry` and add to `OutputLines`
- [x] 3.3 Add `ClearOutputCommand` (`RelayCommand`) that calls `OutputLines.Clear()` and resets `StatusText`
- [x] 3.4 Add `IsLogPanelExpanded` bool property (default `true`) for collapse/expand binding

## 4. Converters & Highlighting

- [x] 4.1 Create `LogLevelToBrushConverter` (`IValueConverter`) that maps `LogEventLevel` → `IBrush` via `LogPanelPalette`
- [x] 4.2 Create `LogMessageHighlighter` utility class that tokenises a message string into `IEnumerable<Inline>` runs — quoted strings, numeric literals, and plain text — each colored from `LogPanelPalette`
- [x] 4.3 Create `HighlightedTextBehavior` attached behavior (or custom control) that binds a `LogEntry` to `SelectableTextBlock.Inlines` via `LogMessageHighlighter`

## 5. Layout & View

- [x] 5.1 Restructure `MainWindow.axaml` tab `ContentTemplate` from `DockPanel` to two-column `Grid` with `GridSplitter` (left: parameters + execute bar, right: log panel)
- [x] 5.2 Build log panel header `DockPanel` with "Log" label, Clear button (`ClearOutputCommand`), and collapse toggle button (`IsLogPanelExpanded`)
- [x] 5.3 Implement collapse behavior — bind right `Grid` column width to `IsLogPanelExpanded` (expanded → `*`, collapsed → `0`)
- [x] 5.4 Replace log `ItemsControl` with `ListBox`/`ItemsRepeater` using `SelectableTextBlock` + `HighlightedTextBehavior` for each log line
- [x] 5.5 Add timestamp `TextBlock` (formatted `HH:mm:ss.fff`, foreground from `LogPanelPalette.TimestampBrush`) to the log line `DataTemplate`
- [x] 5.6 Register `LogLevelToBrushConverter` as static resource and wire it to log line foreground

## 6. Copy & Context Menu

- [x] 6.1 Add `ContextMenu` on the log panel with "Copy" and "Copy All" menu items
- [x] 6.2 Implement "Copy All" command that joins all `OutputLines` (with timestamps) and copies to clipboard

## 7. Integration & Verification

- [x] 7.1 Update `gui-command-invocation` integration — verify structured `LogEntry` flows from `DelegateLogSink` through `CommandTabViewModel` to the log panel
- [x] 7.2 Build and verify zero errors / zero warnings
- [x] 7.3 Manual runtime verification — run a command tab, confirm timestamps, level coloring, inline highlighting, collapse/expand, clear, copy
