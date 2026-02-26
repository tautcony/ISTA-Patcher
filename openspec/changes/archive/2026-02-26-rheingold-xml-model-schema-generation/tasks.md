## 1. Generation Tooling Setup

- [x] 1.1 Identify and pin the XSD-to-C# generator tool/version to be used in repo automation.
- [x] 1.2 Add a repository generation entrypoint script that accepts Rheingold schema input and emits deterministic output.
- [x] 1.3 Add tool configuration files/arguments (namespace, nullability, output layout) required for stable regeneration.

## 2. Model Structure Migration

- [x] 2.1 Define and create folder ownership boundaries under `src/ISTAlter/Models/Rheingold/` for generated vs extension partial files.
- [x] 2.2 Regenerate Rheingold model classes from canonical schema input and commit generated outputs.
- [x] 2.3 Move handwritten behavior into human-owned partial extension files and remove manual edits from generated files.

## 3. Validation and CI Enforcement

- [x] 3.1 Add a local validation command that regenerates models and checks for uncommitted diffs.
- [x] 3.2 Add/extend CI to run regeneration validation and fail when generated outputs are stale.
- [x] 3.3 Ensure generated files include clear machine-generated headers to discourage manual edits.

## 4. Verification and Developer Workflow

- [x] 4.1 Add regression checks (compile + representative XML deserialization/round-trip cases) for regenerated models.
- [x] 4.2 Document developer workflow for schema changes (update schema, regenerate, validate, commit).
- [x] 4.3 Validate the full workflow on a clean checkout to confirm reproducibility across environments.
