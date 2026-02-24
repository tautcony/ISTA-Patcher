## Why

In the ISTAvalon GUI, required parameters look identical to optional ones — there is no visual indicator (e.g., an asterisk `*`) to alert the user. Worse, clicking **Execute** with missing required values sends the command straight through, producing cryptic runtime errors instead of a clear validation message. Users need immediate, at-a-glance awareness of which fields are mandatory and a guard that prevents execution until those fields are filled.

## What Changes

- **Add required-field indicator**: Append a red asterisk (`*`) next to every parameter label whose `ParameterDescriptor.IsRequired` is `true`, across all parameter editor templates (string, path, enum, numeric, string-array, bool).
- **Add pre-execution validation**: Before invoking a command, iterate over all parameters, check whether every required parameter has a non-empty/non-default value, and block execution if any are missing.
- **Show validation feedback**: When validation fails, display a clear message (e.g., inline error text or status bar message) listing the missing required parameters, so the user knows exactly what to fix.
- **ViewModel validation support**: Expose an `IsValid` (or equivalent) computed property on the parameter view-model layer so the view and the execution gate can share a single source of truth.

## Capabilities

### New Capabilities
- `gui-required-param-validation`: Covers the visual marking of required parameters with an asterisk indicator and the pre-execution validation gate that prevents command execution when required parameters are missing.

### Modified Capabilities
- `gui-dynamic-parameter-editors`: Parameter editor templates must render a required-field indicator derived from `ParameterDescriptor.IsRequired`.
- `gui-command-invocation`: Command execution must be gated by a validation check on required parameters, with user-facing feedback on failure.

## Impact

- **Views**: [src/ISTAvalon/Views/MainWindow.axaml](src/ISTAvalon/Views/MainWindow.axaml) — all `DataTemplate` blocks for parameter types need the asterisk indicator.
- **ViewModels**: [src/ISTAvalon/ViewModels/ParameterViewModel.cs](src/ISTAvalon/ViewModels/ParameterViewModel.cs) — add validation logic (e.g., `HasValue` / `IsValid` property) per parameter type.
- **ViewModels**: [src/ISTAvalon/ViewModels/CommandTabViewModel.cs](src/ISTAvalon/ViewModels/CommandTabViewModel.cs) — add validation gate before `ExecuteCommandAsync`; surface validation error text.
- **Models**: [src/ISTAvalon/Models/ParameterDescriptor.cs](src/ISTAvalon/Models/ParameterDescriptor.cs) — no schema changes needed (`IsRequired` already exists).
- **Services**: [src/ISTAvalon/Services/CommandExecutionService.cs](src/ISTAvalon/Services/CommandExecutionService.cs) — no changes expected; validation happens before this layer.
