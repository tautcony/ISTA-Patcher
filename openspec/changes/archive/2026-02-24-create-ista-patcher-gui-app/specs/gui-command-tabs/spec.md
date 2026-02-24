## ADDED Requirements

### Requirement: GUI SHALL expose CLI commands as tabs
The GUI SHALL render a tabbed workspace where each supported CLI command is represented by exactly one top-level tab, and tab labels MUST use the command name resolved from CLI metadata.

#### Scenario: Render tabs from command metadata
- **WHEN** the GUI initializes command descriptors from the CLI command assembly
- **THEN** the UI displays one tab per discovered supported command with the expected command name

### Requirement: GUI SHALL support command tab switching without state loss
The GUI SHALL allow users to switch between command tabs and MUST preserve unsent parameter values for each tab during the same application session.

#### Scenario: Switch tabs and keep draft values
- **WHEN** a user enters parameter values on one command tab and switches to another tab and back
- **THEN** the previously entered values remain available on the original tab
