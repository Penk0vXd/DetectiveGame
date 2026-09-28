# Core gameplay loop

**Status:** `PROPOSED` loop, изграден върху `DECIDED` pillars; изисква prototype validation.

## Candidate full-game loop

```text
Detective office
→ case/call/information
→ избор на location или investigative action
→ crime scene, interviews, body/people/record inspection
→ observations, sources и claims
→ office tools
→ evidence/timeline organization
→ contradictions и theories
→ follow-up questions/locations
→ further investigation
→ accusation/case conclusion
→ consequences
→ next case и potential overarching mystery
```

## Player ownership

Играчът решава какво да inspect-не, кои sources да приеме за надеждни, какво да сравни, каква interpretation да изгради, кога да продължи и кога да обвини.

Играта управлява physical availability, narrow observations, source metadata, consequences и formal validation само когато одобреният design го изисква. Тя не прави decisive inference тихо.

## Information loop

```text
Source → observation/claim → player knowledge → comparison
→ interpretation/theory → follow-up action → new source/consequence
```

## Must happen

- Редуване между gathering и interpreting information.
- Player choices определят какво се сравнява и разследва.
- Връщането в office създава reasoning opportunities.
- Follow-up actions имат разбираема основа.

## Must not happen

- All clues collected → automatic solution.
- Всеки clue отключва точно една highlighted next action.
- Progress изисква exhaust на всички dialogue lines и hotspots.
- Final conclusion е detached quiz, който игнорира investigation-а.
- Backtracking добавя време без decision value.

Office Prototype тества само office-tool и information-comparison сегмента, не целия loop.

