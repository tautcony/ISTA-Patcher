## Context

ISTAvalon is an Avalonia-based GUI that discovers CLI commands via reflection and generates parameter editors dynamically. The `ParameterDescriptor` model already carries an `IsRequired` flag derived from `CliOptionAttribute.Required` / `CliArgumentAttribute.Required`, but neither the view layer nor the execution path uses it. Users currently see no distinction between required and optional parameters, and can execute commands with missing required values—leading to runtime failures.

The MVVM stack is: `ParameterDescriptor` → `ParameterViewModel` (+ typed subclasses) → AXAML `DataTemplate`s in `MainWindow.axaml`. Command execution lives in `CommandTabViewModel.ExecuteCommandAsync`, which delegates to `CommandExecutionService`.

## Goals / Non-Goals

**Goals:**
- Visually mark required parameters with a red asterisk (`*`) in every parameter editor template.
- Add an abstract `HasValue` check to `ParameterViewModel` so each subclass reports whether its current value is "filled".
- Gate command execution: before calling `CommandExecutionService`, validate that all required parameters have values; if not, show a clear status message listing the missing ones and abort.
- Keep the validation logic entirely in the ViewModel layer—no changes to `CommandExecutionService` or the model layer.

**Non-Goals:**
- Real-time inline validation (red borders, per-field error messages). This could be a follow-up; the first iteration uses the status bar.
- Disabling the Execute button when validation fails (the button remains enabled so the user gets explicit feedback on click).
- Validating value correctness beyond "is it filled?" (e.g., path existence, numeric ranges).
- Changing `ParameterDescriptor` or `CommandDiscoveryService`—`IsRequired` is already populated.

## Decisions

### 1. Abstract `HasValue` property on `ParameterViewModel`

**Decision**: Add `public abstract bool HasValue { get; }` to `ParameterViewModel`. Each subclass implements it based on its value semantics.

| Subclass | `HasValue` logic |
|---|---|
| `BoolParameterViewModel` | Always `true` (a bool always has a value) |
| `EnumParameterViewModel` | `SelectedValue != null` |
| `NumericParameterViewModel` | Always `true` (zero is a valid value) |
| `StringParameterViewModel` | `!string.IsNullOrWhiteSpace(TextValue)` |
| `PathParameterViewModel` | `!string.IsNullOrWhiteSpace(TextValue)` |
| `StringArrayParameterViewModel` | `!string.IsNullOrWhiteSpace(TextValue)` |

**Rationale**: A single abstract property keeps validation logic co-located with value logic in each subclass, and gives `CommandTabViewModel` a uniform way to check readiness without type-switching.

**Alternative considered**: A standalone validator class that inspects `GetValue()` return. Rejected because null-checking `GetValue()` is fragile (numeric returns `0` which is valid, bool returns `false` which is valid), so per-type semantics are needed anyway.

### 2. Validation gate in `CommandTabViewModel.ExecuteCommandAsync`

**Decision**: At the top of `ExecuteCommandAsync`, collect all parameters where `Descriptor.IsRequired && !HasValue`. If any exist, set `StatusText` to a message listing the missing parameter display names, and return immediately without executing.

**Rationale**: This is the simplest insertion point—one `if` block before the existing execution logic. The status bar is already used for outcome feedback, so reusing it for validation errors is natural and consistent.

**Alternative considered**: Using `ICommand.CanExecute` to disable the button. Rejected as a non-goal; explicit feedback on click is more discoverable for users who may not understand why the button is greyed out.

### 3. Red asterisk via `TextBlock` `Inlines` in AXAML templates

**Decision**: For each parameter `DataTemplate` that shows a label `TextBlock`, replace the plain `Text="{Binding Descriptor.DisplayName}"` with an `Inlines`-based approach that conditionally appends a red `*` Run when `Descriptor.IsRequired` is `true`. Specifically, use an `<InlineUIContainer>` with a `TextBlock` bound to `IsRequired` visibility, or more simply, add a second `TextBlock` with `Text=" *"` and `Foreground="Red"` whose `IsVisible` is bound to `Descriptor.IsRequired`, placed in a horizontal `StackPanel` alongside the display name.

For the `BoolParameterViewModel` template (checkbox), the asterisk goes into the `Content` area using a similar horizontal panel arrangement.

**Rationale**: This approach requires no new converters or value converters—just an `IsVisible` binding on the asterisk `TextBlock`. It's purely AXAML, keeping code-behind minimal.

**Alternative considered**: A custom `IValueConverter` that appends `" *"` to the display name string. Rejected because it loses the ability to color only the asterisk red.

### 4. Validation message format

**Decision**: When required parameters are missing, set `StatusText` to:
```
⚠ Required: <param1>, <param2>, ...
```
using the parameter `DisplayName` values, comma-separated.

**Rationale**: Consistent with existing status patterns (`✓`, `⚠`, `✗`). Concise and scannable.

## Risks / Trade-offs

- **[Risk] Bool/Numeric required parameters are always "valid"** → These types inherently have a value (false / 0). If the CLI truly requires a non-default value for these, this validation won't catch it. Mitigation: this mirrors CLI behavior where bool flags don't require explicit setting. Accepted as matching current CLI semantics.
- **[Risk] Label layout shift from asterisk insertion** → Adding a `StackPanel` around each label adds minor layout overhead. Mitigation: the panels are lightweight; tested visually with existing templates shows no visible shift.
- **[Trade-off] Status bar vs. inline errors** → Using only the status bar for validation feedback is less polished than per-field error highlighting, but it's significantly simpler and consistent with existing UX patterns. Inline validation can be added later.
