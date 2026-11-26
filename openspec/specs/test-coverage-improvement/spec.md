## Purpose

Describe how coverage gaps are selected, tested, and verified so coverage improvements raise confidence without adding machine-specific or flaky tests.

> Process/historical living spec. Baseline path references the 2026-06-05 improvement work; new coverage work may choose a newer baseline artifact under `TestResults/`.

## Requirements

### Requirement: Coverage Improvements SHALL Be Driven By Reported Gaps

The project SHALL use generated coverage artifacts to select focused test targets, prioritizing uncovered deterministic functions before environment-dependent integration paths.

#### Scenario: Candidate functions are selected from a coverage report

- **WHEN** a coverage report identifies uncovered methods
- **THEN** the plan lists candidate functions with their current uncovered status and expected test approach

#### Scenario: Deterministic functions are prioritized

- **WHEN** uncovered functions include both pure parsing logic and real-environment patching logic
- **THEN** the plan prioritizes pure parsing, formatting, serialization, and in-memory fixture tests before tests that require OS state, real ISTA files, telemetry, or live servers

### Requirement: Coverage Tests SHALL Remain Stable Across Developer Machines

Coverage-improving tests SHALL avoid assumptions about machine-specific resources unless those resources are isolated by fixtures or explicit test seams.

#### Scenario: Environment variables are tested

- **WHEN** tests modify environment variables for GUI observation options
- **THEN** each modified variable is restored after the test completes

#### Scenario: Available ports are tested

- **WHEN** tests call available-port discovery
- **THEN** assertions avoid depending on one exact globally available port unless the test controls that port state

#### Scenario: Cryptographic license verification is tested

- **WHEN** tests generate and validate license signatures
- **THEN** tests assert validity and tamper failure without depending on fixed generated signature bytes

### Requirement: Coverage Progress SHALL Be Verified Against A Baseline

The project SHALL compare new coverage results against the baseline run used to create the plan before considering a coverage-improvement change complete.

#### Scenario: Coverage run is regenerated

- **WHEN** planned tests are implemented
- **THEN** `python scripts/test-coverage.py --html` is run and produces a new Cobertura XML and HTML report

#### Scenario: Coverage deltas are reviewed

- **WHEN** the new coverage report is available
- **THEN** package and class coverage is compared against the agreed baseline under `TestResults/` (historical baseline for the 2026-06-05 change: `TestResults/20260605-203248`)
