## Why

ISTA-Patcher currently exposes powerful functionality only through the CLI, which raises the usage barrier for users who prefer guided interaction and visual configuration. A GUI is needed now to make command-based workflows easier to discover, safer to configure, and faster to execute across platforms.

## What Changes

- Add a new AvaloniaUI desktop application as the GUI entry for ISTA-Patcher workflows.
- Build a tabbed command surface where each CLI command maps to a dedicated tab.
- Introduce reflection-based command metadata discovery from `ISTA-Patcher` command classes and CLI attributes.
- Generate parameter editors dynamically for most option/argument types (boolean, enum, numeric, string, array-like string input).
- Support command execution from the GUI by translating form state into CLI-style invocation parameters.
- Keep GUI behavior aligned with existing CLI contracts so command definitions remain the primary source of truth.

## Capabilities

### New Capabilities
- `gui-command-tabs`: Provide a tabbed GUI layout that maps each supported CLI command to a dedicated user-facing workspace.
- `gui-dynamic-parameter-editors`: Generate command parameter controls dynamically from reflected CLI metadata, minimizing hard-coded per-command UI logic.
- `gui-command-invocation`: Execute selected commands from GUI input state and surface execution outcomes to users.

### Modified Capabilities
- None.

## Impact

- Affected code: new `src/ISTAvalon` Avalonia project, command metadata/reflection layer, dynamic form rendering layer, and command execution bridge.
- Affected integration: project references between GUI app and existing `src/ISTA-Patcher` command assembly.
- Dependencies: AvaloniaUI packages and related MVVM/runtime dependencies for desktop UI.
- Systems: desktop runtime behavior on supported platforms (Windows/macOS/Linux), while preserving existing CLI entry behavior.
