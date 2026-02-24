## MODIFIED Requirements

### Requirement: GUI SHALL execute commands from current form state
The GUI SHALL translate the active tab's parameter values into command invocation inputs and MUST execute the corresponding CLI command flow using the existing command behavior contract. Before execution, the GUI SHALL validate that all required parameters have non-empty values. If validation fails, the GUI MUST abort execution and display a status message listing the missing required parameter display names.

#### Scenario: Execute command with selected parameters
- **WHEN** a user triggers execution from a command tab after providing parameter values and all required parameters have values
- **THEN** the GUI invokes the matching command flow with equivalent effective inputs

#### Scenario: Block execution when required parameters are missing
- **WHEN** a user triggers execution from a command tab and one or more required parameters lack values
- **THEN** the GUI does not invoke the command and displays a warning in the status area listing the missing required parameter display names
