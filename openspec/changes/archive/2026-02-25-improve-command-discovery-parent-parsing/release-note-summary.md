## Release Note Summary

### Command discovery behavior
- Command discovery no longer assumes a single root command and now builds hierarchy from actual command relationships.
- `CliCommand.Parent` is resolved for non-nested command classes.
- For nested command classes, nesting relationship is authoritative and `CliCommand.Parent` is ignored.
- Commands with missing or filtered parents remain discoverable as root-level entries.

### Parameter model changes
- Command parameter metadata now includes inherited parameters from base command types.
- Same-name conflicts between inherited parameters now use a deterministic rule: derived definitions override base definitions.

### Visibility and filtering
- Hidden commands are now excluded by default from GUI command discovery.
- Discovery now supports an explicit include-hidden override for call sites that need hidden commands.

### GUI behavior
- Top-level tabs now represent root executable commands only.
- Child commands are exposed under the parent tab via subcommand selection instead of becoming duplicate top-level tabs.
- Command invocation continues to validate required visible parameters and now works with inherited parameter metadata.

### Tests
- Added regression coverage for multi-root hierarchy handling, nested vs explicit parent precedence, inherited parameter merging, hidden filtering defaults/override, and command-tab hierarchy consumption.
