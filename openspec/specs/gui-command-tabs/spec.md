## Purpose

Describe how discovered root CLI commands appear as GUI tabs and how tab/subcommand state is preserved.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: GUI SHALL expose discovered root commands as tabs

`MainWindowViewModel` SHALL create one `CommandTabViewModel` per root descriptor returned by command discovery (default: hidden excluded, `server` hard-excluded). Tab labels SHALL use the descriptor `Name`.

Child commands SHALL appear as entries in the tab's `AvailableCommands` flat list (root first, then depth-first subcommands) and as a subcommand ComboBox when `HasSubcommands` is true (more than one entry). Child commands MUST NOT become additional top-level tabs when they have a visible parent in the discovery graph.

Root ordering prefers the fixed sequence `patch`, `ilean`, `crypto` when those names are present; other roots follow discovery ordering rules for remaining names.

#### Scenario: Render top-level tabs from root commands

- **WHEN** the GUI initializes from default discovery
- **THEN** the UI displays one top-level tab per discovered root command (e.g. patch, ilean, crypto) and does not show hard-excluded roots such as server

#### Scenario: Render child commands under parent tab

- **WHEN** discovery resolves a command as a child of another command
- **THEN** the GUI presents that command under the parent tab's subcommand list and does not create a separate top-level tab for it

### Requirement: GUI SHALL support command tab switching without state loss

The GUI SHALL allow users to switch between command tabs. Each tab retains its own parameter collections per command type (`_parameterStateByCommand`), selected subcommand, log lines, status text, and log-panel expanded flag for the lifetime of the session ViewModel.

#### Scenario: Switch tabs and keep draft values

- **WHEN** a user enters parameter values on one command tab and switches to another tab and back
- **THEN** the previously entered values remain available on the original tab
