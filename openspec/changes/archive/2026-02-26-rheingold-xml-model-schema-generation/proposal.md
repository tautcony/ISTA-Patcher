## Why

`src/ISTAlter/Models/Rheingold/` currently contains a large, annotation-heavy hand-written XML model surface (about 706 lines with many `[Xml*]`, `[Data*]`, and `[Serializable]` attributes), which is highly likely to be schema-derived and expensive to maintain manually. We need a schema-driven generation flow now to reduce model drift, avoid attribute mistakes, and make future schema updates safer and faster.

## What Changes

- Introduce an automated XSD-to-C# generation pipeline for Rheingold XML models.
- Make generated model files the source of truth for schema-derived types under `src/ISTAlter/Models/Rheingold/`.
- Keep manual code only in extension `partial` classes so custom behavior survives regeneration.
- Add scriptable/CI-executable generation steps so model updates are reproducible and verifiable.
- Define validation checks to detect stale generated output when schema or generation settings change.

## Capabilities

### New Capabilities
- `rheingold-xml-schema-generated-models`: Generate and validate Rheingold XML model classes from schema with safe partial-class extensibility and CI-friendly automation.

### Modified Capabilities
- None.

## Impact

- Affected code: `src/ISTAlter/Models/Rheingold/**` and supporting generation scripts/tooling.
- Build/CI: adds a generation and validation step that can run locally and in CI.
- Developer workflow: schema/model updates shift from manual editing to regenerate-and-extend.
- Risk profile: lowers ongoing maintenance risk from manual attribute synchronization, while introducing dependency on generation tooling configuration.
