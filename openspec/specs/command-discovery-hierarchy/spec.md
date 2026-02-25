## ADDED Requirements

### Requirement: Command discovery SHALL build hierarchy from actual parent relationships
The command discovery layer SHALL construct command hierarchy from the full discovered command set without assuming a single root command. For nested command classes, the discovery layer MUST derive parent-child relationship from type nesting and MUST ignore `CliCommand.Parent`. For non-nested command classes with `CliCommand.Parent` set, the discovery layer MUST derive parent-child relationship from the declared `Parent` target.

#### Scenario: Resolve parent for nested command type
- **WHEN** a discovered command is declared as a nested class and also declares `CliCommand.Parent`
- **THEN** the discovery result uses the nesting parent and ignores `CliCommand.Parent`

#### Scenario: Resolve parent for non-nested command with Parent
- **WHEN** a discovered non-nested command declares `CliCommand.Parent`
- **THEN** the discovery result assigns the command under the declared parent command

#### Scenario: Keep multiple root commands
- **WHEN** discovered commands contain multiple commands with no resolved parent
- **THEN** the discovery result contains all of them as root commands without synthetic reparenting

### Requirement: Command discovery SHALL merge inherited parameter definitions
For each resolved command, the discovery layer SHALL include parameter definitions from the full inheritance chain of the command type. When multiple inherited levels define a parameter with the same effective name, the most-derived definition MUST override base definitions.

#### Scenario: Include base class parameters
- **WHEN** a command type inherits from a base type that declares CLI parameters
- **THEN** the resolved command parameter list includes both base and derived parameters

#### Scenario: Override same-name inherited parameter
- **WHEN** both base and derived command types declare a parameter with the same effective name
- **THEN** the resolved parameter metadata uses the derived declaration

### Requirement: Command discovery SHALL exclude hidden commands by default
The command discovery layer SHALL treat hidden commands as excluded by default in the same filtering stage as `ExcludedCommands`. The discovery API MUST provide an explicit override to include hidden commands when required by callers.

#### Scenario: Hidden command excluded by default
- **WHEN** command discovery runs with default filtering options
- **THEN** commands marked hidden are not present in the returned command set

#### Scenario: Hidden command included with explicit override
- **WHEN** command discovery runs with explicit include-hidden override enabled
- **THEN** commands marked hidden are included in the returned command set
