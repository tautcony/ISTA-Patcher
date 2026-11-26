## Purpose

Describe required-parameter marking and pre-execution validation in the GUI.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: GUI SHALL visually mark required parameters with an asterisk indicator

The GUI SHALL display a red asterisk (`*`) adjacent to the label of every parameter whose descriptor indicates it is required. The asterisk MUST appear for all parameter editor types (string, path, enum, numeric, string-array, bool).

#### Scenario: Required string parameter shows asterisk

- **WHEN** a command tab displays a string parameter that is marked as required
- **THEN** the parameter label includes a red asterisk (`*`) next to the display name

#### Scenario: Optional parameter does not show asterisk

- **WHEN** a command tab displays a parameter that is not marked as required
- **THEN** the parameter label displays only the display name with no asterisk

#### Scenario: Required indicator appears across all editor types

- **WHEN** a command has required parameters of different kinds (bool, enum, numeric, path, string, string-array)
- **THEN** each required parameter's editor template renders the red asterisk indicator consistently

### Requirement: ParameterViewModel SHALL expose a HasValue property for validation

Each `ParameterViewModel` subclass SHALL expose `HasValue` as follows:

- Bool and Numeric: always `true`
- String, Path, and StringArray: `true` only when the text value is not null or whitespace
- Enum: `true` only when `SelectedValue` is non-null

#### Scenario: String parameter with empty value reports no value

- **WHEN** a required string parameter has a null or whitespace-only text value
- **THEN** `HasValue` returns `false`

#### Scenario: String parameter with text reports has value

- **WHEN** a required string parameter has a non-empty, non-whitespace text value
- **THEN** `HasValue` returns `true`

#### Scenario: Bool parameter always reports has value

- **WHEN** a bool parameter exists regardless of its checked state
- **THEN** `HasValue` returns `true`

#### Scenario: Enum parameter with no selection reports no value

- **WHEN** a required enum parameter has no selected value (null)
- **THEN** `HasValue` returns `false`

### Requirement: GUI SHALL validate required parameters before command execution

Before calling the execution service, `CommandTabViewModel` SHALL collect parameters where `Descriptor.IsRequired && !HasValue` and abort if any exist. Status text format:

`⚠ Required: {DisplayName1}, {DisplayName2}, ...`

using each parameter's `Descriptor.DisplayName`.

#### Scenario: Block execution when required parameters are missing

- **WHEN** a user triggers command execution and one or more required parameters have no value
- **THEN** the GUI does not invoke the command, and the status area lists the missing required display names

#### Scenario: Allow execution when all required parameters are filled

- **WHEN** a user triggers command execution and all required parameters have values
- **THEN** the GUI proceeds with command execution

#### Scenario: Validation message format

- **WHEN** required parameters with display names `target-path` and `output-dir` are missing values
- **THEN** the status area displays `⚠ Required: target-path, output-dir`
