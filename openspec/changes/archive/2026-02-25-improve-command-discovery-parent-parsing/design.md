## Context

`CommandDiscoveryService` currently models command resolution as an approximate single-root tree, which does not align with the real `CliCommand` model:
- Commands can point to a non-nested parent via `Parent`.
- A command set can naturally contain multiple roots.
- Parameter definitions may come from an inheritance chain, not only the declaring type.
- `ExcludedCommands` does not currently treat `hidden` as a default exclusion, so the UI may show commands that should remain hidden.

This change affects both command hierarchy construction and parameter visibility, across discovery, mapping, and GUI consumer layers.

## Goals / Non-Goals

**Goals:**
- Build a command graph from the actual command set, supporting multiple roots with stable output ordering.
- Correctly resolve `CliCommand.Parent`:
  - Non-nested classes can define explicit parent-child relationships via `Parent`.
  - Nested classes ignore `Parent` and keep nesting-based parent resolution.
- Merge inherited parameter definitions into a final parameter set with deterministic conflict handling.
- Include `hidden` in `ExcludedCommands` default filtering, while allowing explicit caller override.
- Add regression coverage for core resolution paths.

**Non-Goals:**
- Do not change command execution semantics (discovery/presentation metadata only).
- Do not introduce new command declaration attributes.
- Do not redesign GUI visuals or interaction patterns in this change; only ensure consistent input data semantics.

## Decisions

1. Use a command graph instead of a single-root assumption
- Decision: collect all command nodes first, then resolve parent relationships, and allow multiple roots in the output.
- Rationale: this matches real definitions and avoids incorrectly attaching isolated/cross-file subcommands to a synthetic root.
- Alternative considered: keep a single root and attach everything else under a virtual root.
  - Rejected because it hides real structure and increases UI/testing interpretation cost.

2. Parent resolution precedence
- Decision: resolve parent relationship in this order:
  1) If command type is nested, use nesting parent and ignore `Parent`;
  2) Otherwise, if `Parent` is declared, resolve via `Parent`;
  3) Otherwise treat as a root command.
- Rationale: aligns with `CliCommand.Parent` semantics and preserves compatibility with nested command definitions.
- Alternative considered: always prioritize `Parent`.
  - Rejected because it can break implicit nested relationships and conflicts with the attribute contract.

3. Inherited parameter merge strategy
- Decision: aggregate parameters along the inheritance chain from base to derived; for name conflicts, derived definition overrides base; keep deterministic order for stable UI rendering.
- Rationale: satisfies inherited-parameter visibility while keeping conflict behavior predictable.
- Alternative considered: use only the most-derived type parameters.
  - Rejected because it drops reusable base parameters and fails requirements.

4. `ExcludedCommands` defaults include hidden
- Decision: add exclusion of `hidden=true` commands in the default filtering pipeline, with explicit opt-in override when callers need hidden commands.
- Rationale: default behavior should align with visibility semantics and reduce accidental exposure risk.
- Alternative considered: control hidden behavior only through external config.
  - Rejected because safe defaults are weaker and easy to misconfigure.

5. Test layering
- Decision: add/update unit tests for graph construction and parameter merge rules; use integration tests to validate GUI input model consistency.
- Rationale: rule-heavy resolution is best validated at unit level, while integration tests protect end-to-end behavior.

## Risks / Trade-offs

- [Hierarchy changes alter UI grouping] -> Add regression snapshots and targeted manual verification; document structure changes in release notes.
- [Inherited merge introduces duplicate/override ambiguity] -> Enforce and test the "derived overrides base" rule for same-name parameters.
- [Default hidden filtering impacts tools that relied on hidden commands] -> Provide explicit override and migrate call sites incrementally.
- [Multi-root output ordering instability affects tests] -> Use deterministic ordering for roots and children and update snapshots accordingly.

## Migration Plan

1. Refactor `CommandDiscoveryService` flow into: collect nodes -> resolve parent links -> build command graph -> merge parameters -> apply filtering.
2. Implement parent precedence logic for `Parent` versus nested relationships, with debug-level diagnostics.
3. Implement inherited parameter aggregation and same-name override handling.
4. Update `ExcludedCommands` default strategy and expose override input at call sites.
5. Update tests and required GUI adapters; run regression validation.
6. If compatibility issues appear, temporarily fall back to the old discovery path via a guarded switch/branch.

## Open Questions

- If `Parent` points to a missing or filtered command, should the child be promoted to root or hidden with its parent?
  - Do not promote; the command can exist without its parent.
- Should inherited parameter display order strictly remain "base first, derived last", or be normalized by final declaration order?
  - Derived first.
- Should hidden override be exposed via global config, call-site parameters, or both?
  - Both, with global config taking precedence.
