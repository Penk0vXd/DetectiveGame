# Save и load

**Status:** `DEFERRED` за Office Prototype; requirements са `PROPOSED/OPEN`; не е implemented.

## Purpose

Пази player progress, discovered information, organization, choices и consequences, без да expose-ва hidden case truth.

## Candidate state

Settings/accessibility, campaign progress, per-case knowledge, visited locations/actions, discovered statements/sources, board/timeline organization, theories/deductions, accusations/consequences и office progression.

Static authored case definitions не се копират wholesale във save. Runtime/player state се записва чрез stable IDs.

## Requirements before implementation

Stable identity, case/player state model, slots/autosave/manual expectations, allowed timing, consequence persistence, migration policy и corruption recovery.

## Must happen

- Save не превръща undiscovered truth в player knowledge.
- Load връща coherent world/interaction mode.
- Version changes migrate-ват или fail-ват safely.
- Save error не унищожава тихо valid previous save.
- IDs са stable спрямо presentation changes.

## Must not happen

- Entire live Unity scene като domain save model.
- List position/display title като durable identity.
- Save infrastructure преди meaningful persistent state.
- Multiplayer-oriented architecture.
- Compatibility claim без migration tests.
