## Context

Current ISTA-Patcher capabilities are exposed through a CLI-first architecture in `src/ISTA-Patcher`, with commands defined via DotMake.CommandLine attributes and executed through command classes. The proposed GUI introduces a new desktop entrypoint in `src/ISTAvalon` using AvaloniaUI, while preserving CLI definitions as the contract for behavior and parameter semantics.

This change is cross-cutting because it touches UI composition, command metadata discovery, command invocation flow, and solution/project integration. The primary constraint is to minimize command-specific hardcoding by deriving form behavior from reflected command metadata whenever feasible.

Stakeholders are maintainers (who need a low-maintenance GUI layer aligned with CLI evolution) and end users (who need discoverable command flows and safer parameter input).

## Goals / Non-Goals

**Goals:**
- Provide a tabbed GUI where each supported CLI command maps to one tab.
- Build a dynamic parameter-rendering system from reflected command classes and CLI attributes.
- Support common parameter types with minimal per-command UI customization.
- Execute CLI command flows from GUI input state while keeping command behavior consistent with existing command logic.
- Keep the design maintainable as commands/options evolve in CLI code.

**Non-Goals:**
- Replacing or deprecating the existing CLI entrypoint.
- Rebuilding business logic from command handlers into GUI-specific service implementations.
- Achieving perfect zero-hardcode coverage for every possible/custom type on first iteration.
- Introducing a web frontend or non-Avalonia UI frameworks.

## Decisions

1. Create a dedicated Avalonia project at `src/ISTAvalon` that references `src/ISTA-Patcher`.
- **Why:** Keeps GUI concerns isolated while reusing command definitions and execution logic from the CLI assembly.
- **Alternative considered:** Embedding GUI into the CLI project.
  - **Rejected because:** It mixes concerns, complicates packaging, and makes UI-specific dependencies leak into CLI runtime.

2. Use reflection over command classes and DotMake.CommandLine attributes as the primary metadata source.
- **Why:** CLI attributes already define names, descriptions, required semantics, and argument/option intent. Reusing them enforces single-source-of-truth behavior.
- **Alternative considered:** Maintaining a hand-written GUI form schema.
  - **Rejected because:** High maintenance cost and drift risk when CLI options change.

3. Represent UI fields through an intermediate view-model schema (`CommandDescriptor` + `ParameterDescriptor`) generated from reflection.
- **Why:** Decouples runtime reflection details from Avalonia view bindings and makes control templating predictable.
- **Alternative considered:** Binding directly to reflected `PropertyInfo` in views.
  - **Rejected because:** Hard to validate, test, and extend with UI-level metadata/state.

4. Use type-driven control selection with limited overrides.
- **Default mapping:**
  - `bool` → checkbox
  - `enum` → dropdown
  - numeric primitives → numeric textbox
  - `string` / nullable string → textbox
  - path-like `string` parameters representing directories (e.g., option/argument names or descriptions containing `path`, `directory`, `folder`, and known targets like `TargetPath`) → textbox + folder picker button + drag&drop folder support
  - `string[]` (and simple enumerable string cases) → multiline textbox with delimiter parsing
- **Why:** Covers the majority of current command parameters with deterministic behavior.
- **Alternative considered:** Fully custom controls per parameter.
  - **Rejected because:** Contradicts non-hardcoded requirement.

5. Treat directory path UX as first-class even when CLI type is `string`.
- **Why:** Several CLI inputs are typed as `string` for compatibility but semantically require a directory; users need safer input than manual typing.
- **Implementation direction:** infer directory intent from CLI metadata (attribute name/description/property name heuristics and optional override map), then enable folder selector and drag&drop-to-fill behavior.
- **Alternative considered:** keeping all `string` fields as plain textbox only.
  - **Rejected because:** increases input errors and weakens GUI usability for path-heavy workflows.

6. Build command execution arguments from descriptor state and invoke command pipeline in-process.
- **Why:** Reuses existing CLI command handlers and preserves behavior parity.
- **Alternative considered:** Spawning subprocess and passing serialized args.
  - **Rejected because:** Weaker integration, harder feedback handling, and less testability.

7. Keep an explicit fallback strategy for unsupported/complex parameter types.
- **Why:** Ensures UI remains robust even when reflection encounters non-standard types.
- **Approach:** Render as plain text input with conversion validation; log unsupported details for maintainers.

## Risks / Trade-offs

- [Reflection coupling to attribute shape/runtime behavior] → Mitigation: centralize metadata extraction in one adapter and add defensive fallback behavior.
- [Dynamic control generation can degrade UX for edge-case parameters] → Mitigation: allow targeted per-parameter override hooks without changing global architecture.
- [Directory intent inference for `string` parameters can be wrong] → Mitigation: support explicit per-parameter override metadata and allow manual text editing even when folder picker is enabled.
- [In-process execution can expose UI thread blocking] → Mitigation: run command invocation asynchronously with cancellation support and UI state locking.
- [Command output semantics are currently CLI-oriented] → Mitigation: provide output panel abstraction that captures structured messages and raw text logs.
- [Future command additions may introduce unsupported types] → Mitigation: explicit unsupported-type diagnostics and safe text-based fallback editor.

## Migration Plan

1. Add `src/ISTAvalon` Avalonia project and include it in solution files.
2. Add project reference to `src/ISTA-Patcher` and create bootstrap app shell (main window + tab container).
3. Implement reflection metadata extraction for command descriptors.
4. Implement dynamic parameter control rendering and value conversion pipeline.
5. Wire execution flow and output presentation in GUI.
6. Validate representative commands (`patch`, `ilean`, `crypto`, `server`) and refine fallback coverage.
7. Keep CLI entry unchanged; GUI is an additional entrypoint.

Rollback strategy:
- If GUI integration causes regressions, remove `src/ISTAvalon` from solution/project references and keep CLI-only distribution untouched.

## Open Questions

- Should hidden commands (e.g., `cerebrumancy`) be shown in GUI by default or behind an advanced mode toggle?
    - Yes
- What delimiter and escaping rules should be canonical for array-like inputs in text editors?
    - Use comma-separated values with backslash escaping for commas and backslashes.
- Should command output be normalized into structured events or shown as raw log stream in first release?
    - Show as raw log stream with basic success/failure indication; structured output can be a future enhancement.
- Is there a packaging target where GUI should be distributed separately from CLI binaries?
    - Yes, consider separate packages for `ISTA-Patcher` (CLI) and `ISTA-Avalon` (Avalonia app) to allow users to choose their preferred entrypoint without unnecessary dependencies.
