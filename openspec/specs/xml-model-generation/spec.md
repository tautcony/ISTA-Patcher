## Purpose

Define how Rheingold XML model classes are generated from checked-in XSD schema files while preserving handwritten partial-class behavior.

> Aligned to code as of 2026-07-25.

## Requirements

### Requirement: XSD-backed Rheingold XML Models

Rheingold XML entity classes SHALL be generated at compile time by the `ISTgenerAtor` analyzer (`XsdModelGenerator`) from XSD files included as `AdditionalFiles` under `src/ISTAlter/Schemas/Rheingold/**/*.xsd`.

Generated types are partial classes intended for XML serialization. Handwritten partials under `src/ISTAlter/Models/Rheingold/` extend behavior (e.g. `LicenseInfo.Clone`, dealer master helpers) without replacing the schema contract.

#### Scenario: License XML models are generated

- **GIVEN** license schemas under `Schemas/Rheingold/LicenseManagement` are generator inputs
- **WHEN** `ISTAlter` is compiled
- **THEN** license-related XML model types (including `LicenseInfo` and related schema types) are available and usable with existing serializers

#### Scenario: Dealer data XML models are generated

- **GIVEN** `Schemas/Rheingold/DatabaseProvider/DealerData.xsd` is a generator input
- **WHEN** `ISTAlter` is compiled
- **THEN** dealer data classes/enums from that schema are available for serialization consumers

#### Scenario: Non-schema behavior is preserved

- **GIVEN** generated XML model classes are partial
- **WHEN** callers use handwritten partial members (e.g. license clone helpers, dealer master data helpers)
- **THEN** those behaviors continue to work alongside generated members
