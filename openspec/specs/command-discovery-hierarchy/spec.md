## Purpose

Describe how `CommandDiscoveryService` builds command hierarchy, merges parameters, and filters commands for the GUI.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: Command discovery SHALL build hierarchy from nesting and Parent attributes

The discovery layer SHALL scan types that carry `CliCommandAttribute` and construct a parent/child graph without assuming a single root.

Parent resolution order (`ResolveParentType`):

1. If the command type is nested and its declaring type is also a discovered command, use the declaring type as parent and **ignore** `CliCommand.Parent` (debug-log when Parent was set).
2. Else if `CliCommand.Parent` is set and that type is in the discovered set, use it.
3. Else if Parent points to a missing type, treat as no parent (debug-log).
4. Else root.

If a resolved parent type is not present in the **visible** node map (filtered out), the child remains discoverable as a root (promoted) with a debug log.

#### Scenario: Resolve parent for nested command type

- **WHEN** a discovered command is a nested class whose declaring type is also a command, and it also declares `CliCommand.Parent`
- **THEN** the discovery result uses the nesting parent and ignores `CliCommand.Parent`

#### Scenario: Resolve parent for non-nested command with Parent

- **WHEN** a discovered non-nested command declares `CliCommand.Parent` to another discovered command type
- **THEN** the discovery result assigns the command under that parent

#### Scenario: Keep multiple root commands

- **WHEN** discovered visible commands include multiple commands with no resolvable visible parent
- **THEN** the result contains all of them as roots without synthetic reparenting into a single fake root

### Requirement: Command discovery SHALL merge inherited and parent parameter definitions

For each command, parameters SHALL be collected as:

1. Parent command type hierarchy with `IsParentOption = true` (when a parent type is resolved)
2. Command type hierarchy with `IsParentOption = false`

Within a type hierarchy, most-derived declarations override same-name parameters from bases. Interface-mapped CLI members used by DotMake command classes are included when present on the concrete type/`ICommonPatchOption`-style patterns handled by the builder.

#### Scenario: Include base class parameters

- **WHEN** a command type inherits from a base type that declares CLI parameters
- **THEN** the resolved parameter list includes both base and derived parameters

#### Scenario: Override same-name inherited parameter

- **WHEN** both base and derived command types declare a parameter with the same effective name
- **THEN** the resolved parameter metadata uses the derived declaration

#### Scenario: Include parent options on child commands

- **WHEN** a command has a resolved parent command type
- **THEN** parent CLI options/arguments appear on the child descriptor marked as parent options

### Requirement: Command discovery SHALL exclude hidden commands by default and hard-exclude named commands

Default discovery (`includeHidden: false`) SHALL omit commands whose `CliCommandAttribute.Hidden` is true.

The discovery API SHALL accept `includeHidden: true` to include hidden commands.

Independently of hidden, commands whose resolved name is in the hard-exclude set SHALL always be omitted. The current hard-exclude set is:

- `server`

Only types classified as executable command descriptors (have invokable `RunAsync` per discovery rules) enter the visible set.

#### Scenario: Hidden command excluded by default

- **WHEN** command discovery runs with default filtering
- **THEN** commands marked hidden (e.g. cerebrumancy) are not present in the returned set

#### Scenario: Hidden command included with explicit override

- **WHEN** command discovery runs with `includeHidden: true`
- **THEN** hidden commands are included unless hard-excluded by name

#### Scenario: Server command hard-excluded

- **WHEN** command discovery runs (with or without include-hidden)
- **THEN** the `server` command is not present in the returned set
