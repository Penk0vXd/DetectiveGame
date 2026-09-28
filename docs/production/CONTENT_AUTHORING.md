# Case и content authoring

**Status:** production direction е `PROPOSED`; current content е serialized директно в scene.

## Goal

Cases да могат да се author-ват, review-ват, validate-ват и променят без rewrite на gameplay-control scripts и без смесване на canonical truth с player knowledge.

## Content categories

Case metadata, persons/relationships, locations, canonical timeline, sources, statements, observations, discovery conditions, optional evidence/red herrings, dialogue, formal deductions, consequences и cross-case links.

## Workflow direction

1. Define canonical truth.
2. Define кой какво знае/вярва.
3. Define sources и actual content.
4. Define player access paths.
5. Map required conclusions към fair support.
6. Add plausible wrong theories/red herrings с cause.
7. Validate IDs, references, reachability и timing.
8. Blind playtest.
9. Revise без accidental truth change.

## Validation

Unique stable IDs; no broken references; reachable required sources; no circular unlock deadlock; consistent canonical timeline; fair supporting evidence; explicit mandatory/optional/missable status; valid consequence references.

ScriptableObjects, JSON, custom editor, spreadsheet или друг format са `OPEN`. Изборът идва след малък real case.

## Must not happen

- Full cases в UI controllers.
- Display text като stable identity.
- Large custom editor преди manual case authoring.
- Procedural mysteries преди hand-authored validation.
- AI-generated canon без consistency/fairness review.

