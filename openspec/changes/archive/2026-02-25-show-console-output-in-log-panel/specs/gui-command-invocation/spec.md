## MODIFIED Requirements

### Requirement: GUI SHALL execute commands from current form state
The GUI SHALL translate the active command context's parameter values into command invocation inputs and MUST execute the corresponding CLI command flow using the existing command behavior contract. During execution, the GUI MUST include both structured logging events and execution-scoped captured console stdout/stderr output in its observable output stream. Before execution, the GUI SHALL validate that all required visible parameters have non-empty values. If validation fails, the GUI MUST abort execution and display a status message listing the missing required parameter display names.

#### Scenario: Execute command and surface console output
- **WHEN** a user triggers execution for a command that writes output through `Console` APIs
- **THEN** the GUI invokes the command and shows captured console output in the same execution log stream

#### Scenario: Block execution when required visible parameters are missing
- **WHEN** a user triggers execution and one or more required visible parameters lack values
- **THEN** the GUI does not invoke the command and displays a warning in the status area listing the missing required parameter display names
