## MODIFIED Requirements

### Requirement: GUI SHALL execute commands from current form state
The GUI SHALL translate the active command context's parameter values into command invocation inputs and MUST execute the corresponding CLI command flow using the existing command behavior contract. The active command context MUST include inherited parameter definitions resolved by command discovery. Before execution, the GUI SHALL validate that all required visible parameters have non-empty values. If validation fails, the GUI MUST abort execution and display a status message listing the missing required parameter display names.

#### Scenario: Execute command with inherited parameters provided
- **WHEN** a user triggers execution for a command that includes inherited parameters and all required visible parameters have values
- **THEN** the GUI invokes the matching command flow with effective inputs that include both inherited and command-local parameter values

#### Scenario: Block execution when required visible parameters are missing
- **WHEN** a user triggers execution and one or more required visible parameters lack values
- **THEN** the GUI does not invoke the command and displays a warning in the status area listing the missing required parameter display names

#### Scenario: Hidden command parameters are not required in default mode
- **WHEN** the GUI is operating on command metadata discovered with default filtering
- **THEN** parameters belonging to hidden commands are not surfaced for input or required-value validation
