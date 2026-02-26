## ADDED Requirements

### Requirement: Deterministic Rheingold model generation
The system SHALL generate Rheingold XML model classes from the authoritative XSD input using a repository-defined generation command that produces deterministic output for the same inputs and tool version.

#### Scenario: Re-running generation with unchanged inputs
- **WHEN** a developer runs the documented generation command twice with the same schema files and pinned tool version
- **THEN** the generated file contents are identical between runs

### Requirement: Separation of generated and handwritten model code
The system SHALL isolate machine-generated Rheingold model declarations from handwritten logic by using partial class boundaries so regeneration does not overwrite human-authored extensions.

#### Scenario: Regeneration preserves extension behavior
- **WHEN** generated Rheingold model files are regenerated after schema changes
- **THEN** handwritten partial extension files remain unchanged and continue compiling with the regenerated partial types

### Requirement: CI staleness validation for generated models
The system MUST provide a CI validation step that regenerates Rheingold model outputs and fails if committed generated files are stale relative to schema inputs or generation configuration.

#### Scenario: Pull request contains stale generated files
- **WHEN** a pull request modifies schema or generation configuration without updating committed generated model outputs
- **THEN** CI fails with a clear stale-generated-output signal

### Requirement: Developer workflow for schema updates
The system SHALL document and expose a local workflow that allows developers to regenerate Rheingold model outputs and validate them before commit.

#### Scenario: Developer updates schema intentionally
- **WHEN** a developer changes Rheingold schema inputs and follows the documented regeneration workflow
- **THEN** the developer can produce updated generated model files and complete validation checks locally before pushing changes
