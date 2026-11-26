## Purpose

Describe how parameter editors are generated from reflected CLI metadata and how path-like parameters behave.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: GUI SHALL generate parameter editors from reflected CLI metadata

The GUI SHALL derive parameter editors from `ParameterDescriptor` metadata produced by command discovery (option/argument attributes, property type, required flag, descriptions). Templates in `MainWindow.axaml` cover:

- Bool → checkbox
- Enum → combo box
- Numeric (integral/float) → numeric editor
- Path → `PathEditor`
- String → text box
- StringArray → multiline text box (comma-separated on apply)

Each editor template SHALL render a red asterisk (`*`) when `Descriptor.IsRequired` is true.

Parameters are ordered required-first when building the tab's parameter list.

#### Scenario: Build editors for standard parameter types

- **WHEN** a command descriptor includes boolean, enum, numeric, string, path, and string-array parameters
- **THEN** the GUI generates the corresponding editor templates without command-specific hardcoded views

#### Scenario: Required parameter label includes asterisk indicator

- **WHEN** a parameter editor is generated for a required parameter
- **THEN** the editor label shows a red asterisk next to the display name

#### Scenario: Optional parameter label omits asterisk indicator

- **WHEN** a parameter editor is generated for a non-required parameter
- **THEN** the editor label has no required asterisk

### Requirement: Path kind inference for directory-oriented string parameters

A `string` property is classified as `ParameterKind.Path` when either:

1. Its `CliOption`/`CliArgument` `ValidationRules` include `ExistingDirectory`, or
2. The property name contains `Directory` or `Folder` (case-insensitive)

Other strings remain `ParameterKind.String` (plain text box), even if the name is `TargetPath` **unless** `ExistingDirectory` is set (as on `ICommonPatchOption.TargetPath`).

#### Scenario: ExistingDirectory argument becomes Path editor

- **WHEN** a required CLI argument uses `ValidationRules = ExistingDirectory`
- **THEN** discovery assigns `ParameterKind.Path` and the GUI uses `PathEditor`

### Requirement: GUI SHALL provide directory UX for Path parameters

For `PathParameterViewModel`, the GUI SHALL provide folder browse and drag-and-drop of folders into the path editor, and SHALL allow manual text editing of the path value.

#### Scenario: Populate directory parameter using folder selector

- **WHEN** a user activates folder selection on a Path parameter
- **THEN** the GUI writes the selected folder path into the parameter value

#### Scenario: Populate directory parameter using drag and drop

- **WHEN** a user drops a folder onto a Path parameter editor
- **THEN** the GUI updates the value to the dropped folder path

#### Scenario: Edit path value manually

- **WHEN** a user types in the Path editor after browse or drop
- **THEN** the edited text is retained as the effective parameter value
