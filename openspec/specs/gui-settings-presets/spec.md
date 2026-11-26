## Purpose

Describe preset templates and user preference persistence for ISTAvalon, as implemented by `GuiSettingsService`, `GuiSettings`, and `CommandTabViewModel`.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: GUI SHALL load parameter presets from a settings file

The system SHALL provide a `GuiSettingsService` that loads parameter preset values from `gui-settings.json` next to the application base directory. Presets map top-level command names to property names to string values (`Dictionary<string, Dictionary<string, string>>`).

When a command tab is constructed, the system SHALL pass `settings.GetPresetFor(rootCommandName)` into `CommandTabViewModel`. Matching property names SHALL be applied via `ParameterViewModel.ApplyValue` when parameter editors are first created for a command type.

If `gui-settings.json` is missing or unreadable, `LoadPresets` SHALL fall back to `CreateDefault()`, which provides:

- `Theme = "Default"`
- An in-memory default preset for command `patch` with at least `PatchType = BMW` and `MaxDegreeOfParallelism = 4`

#### Scenario: Apply preset values on tab initialisation

- **WHEN** a command tab is created for a command that has entries in loaded presets
- **THEN** parameter editors for matching property names are pre-filled with those preset values when first built

#### Scenario: Missing preset file uses in-memory defaults

- **WHEN** `gui-settings.json` does not exist at application startup
- **THEN** the system uses `CreateDefault()` (including the default `patch` presets) and continues

#### Scenario: Presets apply only to matching command and property

- **WHEN** presets contain entries for a different command name or property name
- **THEN** those entries do not change unrelated parameter editors

### Requirement: GUI SHALL separate preset template from user preferences

The system SHALL treat `gui-settings.json` as a preset template (build-deployed asset). Runtime user preferences (theme) SHALL be persisted only to `user-preferences.json` via `GuiSettingsService.Save`. Runtime code MUST NOT write to `gui-settings.json`.

On load, `Load` reads presets first, then overlays theme from `user-preferences.json` when present.

#### Scenario: Theme change persists to user-preferences.json

- **WHEN** the user cycles the application theme
- **THEN** the new theme value is written to `user-preferences.json` and `gui-settings.json` is not modified

#### Scenario: User preferences override theme on next launch

- **WHEN** the application starts and `user-preferences.json` exists with a theme value
- **THEN** that theme is applied over the theme field from presets/defaults

#### Scenario: Runtime does not mutate preset file

- **WHEN** any runtime operation (theme change, preset application, command execution) occurs
- **THEN** `gui-settings.json` is not written by `GuiSettingsService`

### Requirement: GUI SHALL support manual preset reset per command tab

Each command tab SHALL expose `ResetToPresetCommand` that re-applies the tab's preset dictionary to **all** parameter collections already created for subcommands on that tab (`_parameterStateByCommand` values). The command's CanExecute is gated on `HasPreset`.

#### Scenario: Reset restores preset values

- **WHEN** a user has modified parameter values and activates Reset while `HasPreset` is true
- **THEN** matching properties on existing parameter collections are restored from the tab's preset dictionary

#### Scenario: Reset is unavailable without presets

- **WHEN** the tab was constructed with a null or empty preset dictionary
- **THEN** `HasPreset` is false and Reset is not executable

### Requirement: GUI SHALL indicate when presets are available

Each command tab SHALL set `HasPreset` to true when the constructor receives a non-null preset dictionary with `Count > 0` for that root command name.

#### Scenario: Preset indicator is true when presets exist

- **WHEN** loaded settings contain at least one property entry for the root command name
- **THEN** `HasPreset` is true

#### Scenario: Preset indicator is false when no presets exist

- **WHEN** loaded settings have no dictionary (or empty) for the root command name
- **THEN** `HasPreset` is false
