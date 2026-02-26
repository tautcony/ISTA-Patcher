## Context

`src/ISTAlter/Models/Rheingold/` currently mixes schema-shaped DTO concerns and manual maintenance, with many serialization attributes that must stay aligned with upstream XML schema. This creates frequent drift risk when schema evolves and raises review cost because low-value generated structure and high-value handwritten logic are interleaved.

The proposal defines one new capability: `rheingold-xml-schema-generated-models`. The design must provide deterministic generation, preserve extensibility via `partial` classes, and make staleness detectable in CI.

## Goals / Non-Goals

**Goals:**
- Establish a repeatable XSD-to-C# generation workflow for Rheingold models.
- Ensure generated files can be fully replaced without losing manual logic.
- Make generation runnable both locally and in CI with consistent output.
- Add a validation mechanism that fails when committed generated code is stale.

**Non-Goals:**
- Redesigning Rheingold domain semantics or XML contract shape.
- Introducing behavioral logic into generated files.
- Migrating unrelated model folders outside `src/ISTAlter/Models/Rheingold/`.
- Solving every historical serializer inconsistency in one pass.

## Decisions

1. Use a dedicated generation entrypoint script committed in-repo.
Rationale: keeps tool invocation and parameters centralized, versioned, and auditable.
Alternatives considered:
- Manual IDE/tool invocation: rejected due to non-repeatable output and hidden local settings.
- Ad hoc CI-only script: rejected because developers need local regeneration parity.

2. Split generated and handwritten code via `partial` classes.
Rationale: generated files can be overwritten safely while extension behavior remains in stable hand-maintained files.
Alternatives considered:
- Keep everything generated with no extension points: rejected because custom logic would be brittle.
- Keep everything handwritten: rejected because it preserves current drift and annotation error risks.

3. Store generated outputs in source control.
Rationale: avoids requiring generation tooling at runtime/consumer build, enables diff review of schema-impacting changes, and supports deterministic CI validation.
Alternatives considered:
- Generate during every build: rejected due to added build fragility and toolchain coupling.

4. Add CI stale-check by regenerating and diffing tracked model outputs.
Rationale: catches forgotten regeneration before merge and enforces deterministic output.
Alternatives considered:
- Rely on reviewer discipline only: rejected as too error-prone.

5. Define a stable folder convention under `src/ISTAlter/Models/Rheingold/`:
- generated files: machine-owned (overwrite-safe)
- extension partials: human-owned
Rationale: ownership boundaries reduce accidental edits and merge conflicts.

## Risks / Trade-offs

- [Generation tool output may vary by version/platform] -> Pin tool version and enforce in script/CI environment.
- [Large generated diffs reduce review signal] -> Keep extension logic separate and optionally split generated files per schema area to improve diff locality.
- [Developers may accidentally edit generated files] -> Add header comments in generated files and document ownership rules.
- [Initial migration may miss edge-case annotations] -> Validate with representative XML round-trip/deserialization tests before full adoption.

## Migration Plan

1. Add generation script and tooling configuration, with pinned version.
2. Introduce folder/file ownership convention (generated vs extension partials).
3. Regenerate Rheingold model files from authoritative schema and commit outputs.
4. Move existing handwritten behavior/utility members into extension partial files.
5. Add CI job/step to run generation and fail on uncommitted diffs.
6. Run regression checks using sample XML payloads; confirm no behavioral breakage.
7. Document developer workflow for schema updates (edit schema -> regenerate -> review -> commit).

Rollback strategy:
- Revert generation script/config and generated-file migration commit if regressions are found.
- Temporarily restore previous handwritten model set while retaining investigative test cases.

## Open Questions

- Which XSD source(s) are canonical for Rheingold in this repository, and where should they live?
  - generate from current code
- Which concrete generator/tool should be standardized (feature parity, namespace handling, deterministic output quality)?
  - deterministic output quality
- Should generated code use nullable reference type annotations and which target framework/language version constraints apply?
  - yes
- Do we require one-time compatibility shims if generated naming differs from current handwritten class/member names?
  - yes
