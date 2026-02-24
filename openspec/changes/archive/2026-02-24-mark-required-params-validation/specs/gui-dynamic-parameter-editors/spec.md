## MODIFIED Requirements

### Requirement: GUI SHALL generate parameter editors from reflected CLI metadata
The GUI SHALL derive parameter editors from reflected CLI command metadata (including option/argument attributes, property type, and descriptive metadata) and MUST avoid requiring per-command hardcoded forms for the majority of parameters. Each parameter editor template SHALL render a red asterisk (`*`) indicator adjacent to the parameter label when the parameter's `IsRequired` flag is `true`.

#### Scenario: Build editors for standard parameter types
- **WHEN** a command descriptor includes boolean, enum, numeric, string, and string-array-like parameters
- **THEN** the GUI generates appropriate editors for each parameter type without command-specific hardcoded view logic

#### Scenario: Required parameter label includes asterisk indicator
- **WHEN** a parameter editor is generated for a parameter whose descriptor has `IsRequired` set to `true`
- **THEN** the editor label displays the parameter display name followed by a red asterisk (`*`)

#### Scenario: Optional parameter label omits asterisk indicator
- **WHEN** a parameter editor is generated for a parameter whose descriptor has `IsRequired` set to `false`
- **THEN** the editor label displays only the parameter display name with no asterisk
