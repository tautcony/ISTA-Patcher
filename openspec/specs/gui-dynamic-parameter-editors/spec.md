## ADDED Requirements

### Requirement: GUI SHALL generate parameter editors from reflected CLI metadata
The GUI SHALL derive parameter editors from reflected CLI command metadata (including option/argument attributes, property type, and descriptive metadata) and MUST avoid requiring per-command hardcoded forms for the majority of parameters.

#### Scenario: Build editors for standard parameter types
- **WHEN** a command descriptor includes boolean, enum, numeric, string, and string-array-like parameters
- **THEN** the GUI generates appropriate editors for each parameter type without command-specific hardcoded view logic

### Requirement: GUI SHALL provide enhanced directory input UX for path-like string parameters
For parameters typed as `string` but semantically representing directories, the GUI SHALL provide a folder selector action and MUST support drag-and-drop of folders to populate the parameter value.

#### Scenario: Populate directory parameter using folder selector
- **WHEN** a user activates folder selection on a path-like string parameter
- **THEN** the GUI writes the selected folder path into the parameter editor

#### Scenario: Populate directory parameter using drag and drop
- **WHEN** a user drags a folder from the operating system into a path-like string parameter editor
- **THEN** the GUI accepts the drop and updates the editor value to the dropped folder path

### Requirement: GUI SHALL allow manual override of inferred directory fields
The GUI SHALL allow users to manually edit any path-like string parameter regardless of whether it was inferred as a directory field.

#### Scenario: Edit inferred directory value manually
- **WHEN** a user modifies text in a directory-enabled parameter editor after using folder selection or drag-and-drop
- **THEN** the edited text is retained as the effective parameter value
