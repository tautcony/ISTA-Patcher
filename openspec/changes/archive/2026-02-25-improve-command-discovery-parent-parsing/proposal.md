## Why

The current `CommandDiscoveryService` parses command structure with a single-root assumption, which cannot correctly represent parent-child relationships defined via `CliCommand.Parent`, and it does not fully merge parameter metadata from inheritance chains. This causes command tree structure, parameter presentation, and filtering behavior to diverge from actual command definitions, reducing usability and maintainability.

## What Changes

- Refactor command discovery to remove the single root command assumption and build a command graph from the actual command set.
- Support explicit parent-child binding through `CliCommand.Parent`; when a command type is nested, follow the rule that `Parent` is ignored.
- Complete parameter collection by including inherited parameter definitions and applying deterministic conflict precedence.
- Adjust `ExcludedCommands` behavior so `hidden` is part of default filtering, while still allowing explicit override.
- Add tests for command graph construction and parameter merge behavior, covering multi-root, explicit parent mapping, inherited parameters, and hidden filtering.

## Capabilities

### New Capabilities
- `command-discovery-hierarchy`: Define discovery-stage requirements for multi-root support, `CliCommand.Parent` resolution, inherited parameter merging, and default hidden filtering.

### Modified Capabilities
- `gui-command-tabs`: Update command grouping and hierarchy requirements so the UI reflects the new command tree resolution behavior.
- `gui-command-invocation`: Update parameter source and visibility requirements so inherited parameters and default hidden filtering are consistently applied in the invocation UI.

## Impact

- Affected code is mainly in command discovery and metadata mapping (`CommandDiscoveryService` and related models/adapters).
- Input data for the GUI command tree and parameter panel will change and requires synchronized verification of rendering and interaction behavior.
- Tests will be added/updated to reduce regression risk in parent resolution, inherited parameter merging, and default hidden filtering.
