## 1. Project Setup and Solution Integration

- [x] 1.1 Create the new Avalonia desktop project under `src/ISTAvalon` with required Avalonia packages and assets.
- [x] 1.2 Add `src/ISTAvalon` to solution files (`ISTA-Patcher.sln` and `ISTA-Patcher.slnx`) and verify it builds with existing projects.
- [x] 1.3 Add project references so `ISTAvalon` can access command metadata and execution entrypoints from `src/ISTA-Patcher`.

## 2. Command Metadata Discovery

- [x] 2.1 Implement reflection-based discovery for supported CLI command types and their command-level metadata.
- [x] 2.2 Implement parameter metadata extraction for options/arguments (name, description, required, kind, type, defaults).
- [x] 2.3 Build `CommandDescriptor`/`ParameterDescriptor` models to decouple reflection details from UI rendering.

## 3. Tabbed GUI Shell

- [x] 3.1 Implement the Avalonia main window with a tab container bound to discovered command descriptors.
- [x] 3.2 Ensure one top-level tab is rendered per supported command and tab title uses resolved CLI command name.
- [x] 3.3 Preserve per-tab unsent form state when switching between tabs in the same session.

## 4. Dynamic Parameter Editors

- [x] 4.1 Implement type-driven editor mapping for bool, enum, numeric, string, and string-array-like parameters.
- [x] 4.2 Implement path-like `string` inference heuristics (property name / option name / description based) for directory semantics.
- [x] 4.3 Add folder selector UI for inferred directory parameters and populate values from selected folder paths.
- [x] 4.4 Add drag-and-drop folder support for inferred directory parameters and keep manual text editing available as override.
- [x] 4.5 Implement fallback editor behavior for unsupported/complex parameter types with validation-safe text conversion.

## 5. Command Invocation and Output

- [x] 5.1 Implement argument/value conversion from editor state to command invocation inputs.
- [x] 5.2 Execute selected command flows asynchronously and prevent unsafe concurrent UI execution actions.
- [x] 5.3 Display command execution progress and final outcomes (success/failure) in the GUI output area.

## 6. Verification and Polishing

- [x] 6.1 Validate representative command tabs (`patch`, `ilean`, `crypto`, `server`) for metadata accuracy and execution parity.
- [x] 6.2 Verify directory UX by testing folder selector and drag-and-drop on path-like string parameters (including `TargetPath`).
- [x] 6.3 Run solution build/test checks relevant to changed projects and resolve integration issues within scope.
