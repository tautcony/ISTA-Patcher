## 1. Discovery Model Refactor

- [x] 1.1 Locate current `CommandDiscoveryService` command tree construction path and document existing single-root assumptions in code comments or notes.
- [x] 1.2 Refactor discovery flow to build a command graph from all discovered commands before assigning roots.
- [x] 1.3 Add deterministic ordering for root commands and child commands to keep output stable across runs.

## 2. Parent Resolution Rules

- [x] 2.1 Implement parent resolution precedence: nested-type parent first, explicit `CliCommand.Parent` second, otherwise root.
- [x] 2.2 Add handling for invalid/missing `Parent` targets according to current design decision (child remains discoverable).
- [x] 2.3 Add debug-level diagnostics for parent resolution outcomes to support troubleshooting.

## 3. Parameter Aggregation and Filtering

- [x] 3.1 Implement inherited parameter aggregation along the full base-to-derived command type chain.
- [x] 3.2 Implement same-name parameter conflict handling where derived declarations override base declarations.
- [x] 3.3 Update `ExcludedCommands` default filtering to exclude hidden commands.
- [x] 3.4 Add explicit include-hidden override input and wire it through discovery call sites.

## 4. GUI Metadata Consumption Updates

- [x] 4.1 Update command tab metadata mapping so only root commands become top-level tabs.
- [x] 4.2 Update subcommand mapping so child commands resolved by nesting or `Parent` render under the parent command context.
- [x] 4.3 Update command invocation parameter binding so inherited parameters are included in visible/required validation.
- [x] 4.4 Verify hidden-command default filtering prevents hidden command parameters from appearing in normal GUI flows.

## 5. Tests and Regression Coverage

- [x] 5.1 Add/adjust unit tests for multi-root discovery behavior and deterministic ordering.
- [x] 5.2 Add/adjust unit tests for parent resolution precedence (nested vs explicit `Parent`).
- [x] 5.3 Add/adjust unit tests for inherited parameter inclusion and derived-overrides-base conflict behavior.
- [x] 5.4 Add/adjust unit tests for default hidden exclusion and include-hidden override behavior.
- [x] 5.5 Add/adjust integration/UI-facing tests for tab hierarchy and invocation behavior using inherited parameters.

## 6. Validation and Rollout

- [x] 6.1 Run targeted test suites for discovery, GUI command tabs, and command invocation.
- [x] 6.2 Manually verify representative commands with nested parent, explicit parent, multiple roots, and hidden flags.
- [x] 6.3 Prepare release note/update summary describing hierarchy behavior changes and hidden filtering defaults.
