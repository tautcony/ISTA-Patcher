## 1. ViewModel: Add HasValue abstraction

- [x] 1.1 Add `public abstract bool HasValue { get; }` to the `ParameterViewModel` base class in `src/ISTAvalon/ViewModels/ParameterViewModel.cs`
- [x] 1.2 Implement `HasValue` in `BoolParameterViewModel` — return `true` unconditionally
- [x] 1.3 Implement `HasValue` in `EnumParameterViewModel` — return `SelectedValue != null`
- [x] 1.4 Implement `HasValue` in `NumericParameterViewModel` — return `true` unconditionally
- [x] 1.5 Implement `HasValue` in `StringParameterViewModel` — return `!string.IsNullOrWhiteSpace(TextValue)`
- [x] 1.6 Implement `HasValue` in `PathParameterViewModel` — return `!string.IsNullOrWhiteSpace(TextValue)`
- [x] 1.7 Implement `HasValue` in `StringArrayParameterViewModel` — return `!string.IsNullOrWhiteSpace(TextValue)`

## 2. ViewModel: Add validation gate to CommandTabViewModel

- [x] 2.1 In `CommandTabViewModel.ExecuteCommandAsync`, add a validation check at the top that collects all parameters where `Descriptor.IsRequired && !HasValue`
- [x] 2.2 If missing parameters exist, set `StatusText` to `⚠ Required: <param1>, <param2>, ...` using display names and return early without executing
- [x] 2.3 Verify the Execute button remains enabled (no `CanExecute` change) — validation feedback is on-click only

## 3. View: Add red asterisk indicator to parameter DataTemplates

- [x] 3.1 Update `BoolParameterViewModel` DataTemplate in `MainWindow.axaml` — replace plain `Content` with a `StackPanel` containing the display name and a conditionally visible red `*` TextBlock bound to `Descriptor.IsRequired`
- [x] 3.2 Update `EnumParameterViewModel` DataTemplate — replace label `TextBlock` with a horizontal `StackPanel` containing the display name TextBlock and a conditionally visible red `*` TextBlock
- [x] 3.3 Update `NumericParameterViewModel` DataTemplate — same asterisk pattern as 3.2
- [x] 3.4 Update `PathParameterViewModel` DataTemplate — same asterisk pattern as 3.2
- [x] 3.5 Update `StringParameterViewModel` DataTemplate — same asterisk pattern as 3.2
- [x] 3.6 Update `StringArrayParameterViewModel` DataTemplate — same asterisk pattern as 3.2

## 4. Verification

- [x] 4.1 Build the solution and confirm no compile errors
- [x] 4.2 Manually verify that required parameters show a red asterisk in the GUI
- [x] 4.3 Manually verify that executing with missing required parameters shows the validation warning and does not run the command
- [x] 4.4 Manually verify that executing with all required parameters filled proceeds normally
