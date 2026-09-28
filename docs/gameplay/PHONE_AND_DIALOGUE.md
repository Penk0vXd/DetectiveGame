# Phone и dialogue

**Status:** basic phone panels са `IMPLEMENTED`; investigative dialogue rules са `PROPOSED/OPEN`.

## Purpose

Phone свързва играча с witnesses, suspects, police, forensics, coroner и други sources. Dialogue е investigative action, не само exposition.

## Current implementation

Open/exit, contacts, start call, hang up и един serialized contact/dialogue. Текущото съдържание е non-canonical placeholder.

## Intended capabilities

Normal questions, follow-ups, evidence-dependent questions, contradiction challenges, locked questions, lies/uncertainty/omission и recorded statements. Exact model е `OPEN`.

## Information rules

- Spoken line става Statement/Claim, не fact.
- Original wording и speaker се пазят.
- Unlocked question не доказва автоматично interpretation.
- Contradiction challenge изисква deliberate player choice или approved structured deduction.
- Exhaust-all-dialogue не е dominant strategy.

## Must happen

- Clear contact identity.
- Call може да добави, промени или challenge-не информация.
- Follow-up availability следва understandable rules.
- Phone state/world control се възстановяват надеждно.
- Content постепенно се author-ва извън control-flow code.

## Must not happen

- Click every line strategy.
- Нов въпрос автоматично назовава exact contradiction.
- Character разкрива case truth само защото clue flag е достигнат.
- Voice/animation scope преди dialogue validation.

## Open

Topic list/freeform/evidence presentation; visibility на locked questions; statement review; asynchronous calls; voice scope.

