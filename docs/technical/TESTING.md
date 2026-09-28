# Testing strategy

**Status:** `PROPOSED` strategy; няма authored automated tests.

## Layers

### Static

Versions, scene/prefab script references, missing scripts/null fields, build settings и generated/source consistency. Не доказва runtime.

### Edit Mode

За plain deterministic rules: statement/fact relations, time ranges, discovery state, content ID validation и save migrations.

### Play Mode

За Unity integration: exclusive modes, cursor/control restoration, app open/close, wiring и repeated re-entry.

### Manual runtime

Задължително за Office Prototype чрез `../prototype/ACCEPTANCE_TESTS.md`.

### Human playtest

За discoverability, detective feeling, fairness, pacing и дали UI издава conclusion-а.

## Evidence discipline

- “Compiles” → compilation evidence.
- “Scene wired” → Inspector/static scene evidence.
- “Flow works” → runtime evidence.
- “Player understands” → observed playtest.
- “Case is fair/solvable” → content validation + blind playtest.

## Regression priorities

1. Controls винаги се възстановяват.
2. Само един focused mode owns input.
3. Nested UI exit затваря correct state.
4. Displayed information съвпада със selection.
5. Re-entry е valid.
6. Presentation не mutates case truth.

