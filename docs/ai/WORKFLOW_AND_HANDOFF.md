# AI workflow и handoff protocol

**Status:** active operating protocol.

## Task lifecycle

### 1. Intake

Определи concrete outcome, task type и active statuses.

### 2. Context

Прочети instructions/index/decisions/open questions/task docs и inspect-ни authoritative repository state + git status.

### 3. Conflict check

Провери request vs DECIDED, docs vs implementation, scope vs phase, technical plan vs learning rules и stale version claims. Докладвай conflicts.

### 4. Decision gate

- `IMPLEMENTED` bug с approved expected behavior: fix при request.
- `DECIDED`, но липсващо behavior: implement при request.
- `PROPOSED`: prototype само при explicit authorization.
- P0/P1 `OPEN`: analysis + user decision преди durable implementation.
- `NEEDS RESEARCH`: research преди recommendation.

### 5. Execution

Bounded coherent changes, unrelated work preserved, Unity serialization safety, prototype shortcuts documented, docs updated.

### 6. Verification

Map-ни всяко requirement към evidence. Static, automated, Unity runtime и playtest evidence не се смесват.

### 7. Handoff

```text
Outcome:
Active role(s):
Status changed:
Files changed:
Behavior/design changed:
Decisions preserved:
Verification performed:
Not verified:
Open questions:
Risks/debt:
Recommended next action:
```

## Multi-agent rules

- Non-overlapping ownership.
- Всеки agent получава canonical reading order.
- Specialist output е evidence, не final authority.
- Един coordinator интегрира и verify-ва.
- Не edit-вайте една Unity scene/prefab паралелно.
- “Done” изисква requirement-level evidence.

## Context-compaction handoff

Запиши objective, inspected files, completed changes, decisions/questions, exact test results, next safe action и unrelated dirty files. Не разчитай на conversational memory.

