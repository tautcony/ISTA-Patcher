## MODIFIED Requirements

### Requirement: GUI SHALL expose CLI commands as tabs
The GUI SHALL render a tabbed workspace where each root CLI command is represented by exactly one top-level tab, and tab labels MUST use the command name resolved from CLI metadata. For commands resolved as children through nesting or `CliCommand.Parent`, the GUI MUST present subcommand navigation under the corresponding root command tab and MUST NOT duplicate child commands as additional top-level tabs.

#### Scenario: Render top-level tabs from root commands
- **WHEN** the GUI initializes command descriptors from the CLI command assembly
- **THEN** the UI displays one top-level tab per discovered root command with the expected command name

#### Scenario: Render child commands under parent tab
- **WHEN** discovery resolves a command as a child of another command
- **THEN** the GUI presents that command as a subcommand under the parent command tab and does not create a separate top-level tab for it
