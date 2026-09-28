# Technical architecture

**Status:** current prototype architecture е `IMPLEMENTED`; durable production architecture е предимно `PROPOSED/OPEN`.

## Objective

Investigation systems и case content трябва да растат, без всеки case да се hardcode-ва в scene objects и UI controllers. В същото време prototype-ът остава прост, разбираем и подходящ за обучение.

## Current architecture

```text
Input asset/generated wrapper
→ PlayerInputReader
→ FirstPersonController / PlayerInteraction
→ IInteractable
→ local UI controller или physical document behavior
→ serialized scene content
```

Computer, phone и board използват world-interactable + UI-controller. Computer има power-state controller и малки UI data/entry classes.

### Strengths

- Малка и разбираема структура.
- Shared `IInteractable` contract.
- Explicit open/close UI operations.
- Exit events отделят UI buttons от control restoration.
- Email/case list използват prefabs и simple data.

### Limitations

- Interaction modes управляват control/cursor независимо.
- Direct Inspector references между systems.
- Case/evidence/phone content е в scene components.
- Няма stable IDs, provenance, save/load или player-knowledge separation.
- Няма namespaces, assembly boundaries или authored automated tests.

## Proposed layers

- **Presentation:** GameObjects, UI, animation, audio; показва state и forwards intent.
- **Application/interaction coordination:** modes и use cases.
- **Investigation domain:** cases, sources, facts, statements, events, evidence, knowledge, deductions и accusations.
- **Content/authoring:** designer definitions и validation; storage format е `OPEN`.
- **Persistence:** save schema/migrations преди Vertical Slice.

Preferred dependency direction:

```text
Unity presentation → application/use cases → domain model
content adapters ─────────────────────────→ domain model
persistence adapters ↔ save DTOs ↔ domain state
```

Не налагай тази separation върху Office Prototype без real need.

## Ownership direction

- UI controller не е long-term owner на case truth.
- World interactable не управлява narrative progression.
- Input reader не взема gameplay decisions.
- Cross-system content получава stable IDs преди save/load.
- Player knowledge и objective truth се разделят преди deductions/consequences.

## Prototype vs production

| Concern | Prototype | Production direction |
|---|---|---|
| Content | Small serialized scene lists | Reusable validated case definitions |
| Modes | Local flags | Coordinator при proven complexity |
| IDs | References/list indices | Stable IDs |
| Testing | Manual flows | Domain tests + runtime integration |
| Assembly | `Assembly-CSharp` | Namespaces/asmdefs при real value |
| UI | Scene hierarchy | Reusable views при repetition |

## Must not happen

- Generic enterprise framework преди game rules.
- Event bus, service locator, DI container, ECS или networking за future-proofing.
- Всеки system да чете/пише директно всички scene objects.
- Canonical case truth само във visible UI text.
- Refactor само за fashionable pattern.

## Architecture gate

P1 промяна изисква concrete problem/evidence, player/production impact, simpler alternative, migration risk, test/rollback plan и user approval в `DECISIONS.md`.

