# Investigation data model

**Status:** conceptual model е `PROPOSED`; current prototype използва UI-oriented serializable classes.

## Current implemented data

- `EmailData`: sender, subject, date, body.
- `CaseData`: case number/name/status/date/location/victim/lead detective/summary.
- Board: private nested title/description records в UI controller.
- Phone: direct contact/dialogue strings в UI controller.

Това е достатъчно за UI prototype, но не за final investigation или persistence.

## Proposed domain

```text
Case
├── Persons
├── Locations
├── Sources (documents, photos, objects, reports, conversations)
├── Statements / Claims
├── Observations / Facts
├── Events / Time Ranges
├── Leads
└── Canonical Truth

PlayerCaseState
├── Discovered Sources
├── Recorded Knowledge
├── Notes / Organization
├── Proposed Relationships
├── Deductions
├── Visited / Completed Actions
└── Submitted Conclusion
```

## Required separations

- **Case truth vs player knowledge:** UI работи с discovered knowledge, не с hidden truth.
- **Source vs assertion:** един source може да съдържа много facts/claims.
- **Fact vs interpretation:** narrow observation не съдържа conclusion-а си.
- **Event vs timeline entry:** canonical event и uncertain player representation са различни.

## Candidate stable IDs

`CaseId`, `PersonId`, `LocationId`, `SourceId`, `StatementId`, `ObservationId`, `EventId`, `DeductionId`. Exact type е `OPEN`; не implement-вай всички в Office Prototype.

## Must not happen

- Scene object names или list indices като permanent saved identity.
- Hidden truth директно към UI.
- Narrative progression като scattered booleans без model.
- Един generic `EvidenceData`, който смесва source, claim, fact и interpretation.
- Storage format преди authoring/runtime/save requirements.

Formalize-вай първия real domain slice, когато Investigation Prototype има contradiction между поне два source-а.

