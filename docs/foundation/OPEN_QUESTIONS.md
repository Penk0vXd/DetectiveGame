# Нерешени въпроси

**Status:** canonical source за нерешените решения.

AI може да анализира варианти, но не трябва тихо да избира P0 или P1 отговор.

## P0 — Core gameplay

| ID | Въпрос | Защо е важен | Нужен до |
|---|---|---|---|
| Q-001 | Какво observable player experience прави Office Prototype успешен? | Определя теста и спира feature drift | Validation playtest |
| Q-002 | Какъв е общият interaction language за computer, phone, board, photos и documents? | Controls, cursor states, consistency и accessibility | Office Prototype |
| Q-003 | Каква минимална information sequence свързва email, phone, document и board? | Тества detective feeling, не отделни UI demos | Office content pass |
| Q-004 | Колко interaction highlighting е допустимо? | Баланс между pixel hunting и quest markers | Interaction iteration |
| Q-005 | Freeform, predefined или hybrid е deduction системата? | Централната механика | Investigation Prototype |
| Q-006 | Как представяме facts, claims, observations, interpretations и contradictions? | UI не трябва да прави reasoning | Evidence Prototype |
| Q-007 | Може ли mandatory evidence да бъде пропуснато завинаги? | Fairness и case validation | Crime Scene Prototype |
| Q-008 | Какво се подава при final accusation и как се оценява support? | Meaningful failure и consequences | Vertical Slice |
| Q-009 | Какъв hint model пази deduction? | Accessibility без automatic solving | Investigation playtests |

## P1 — Architecture

- Q-101: Canonical domain model за source, fact, statement, evidence, person, event, location и inference.
- Q-102: Разделяне на objective case truth от player knowledge.
- Q-103: Централно координиране на interaction modes.
- Q-104: Кое prototype code е disposable и кое трябва да остане.
- Q-105: Authoring format за case content.
- Q-106: Save-state granularity и versioning.
- Q-107: Кога са оправдани namespaces и assembly definitions.

## P2 — Production

- Реалистичен брой hand-authored cases.
- Допустимо narrative branching.
- Voice acting scope.
- Content-validation tools за logical solvability.
- Mystery playtesting без изчерпване на tester pool.
- Ниво на forensic/police realism.
- Replayability след узнаване на решението.

## P3 — Polish/later

- Final computer/phone visual language.
- Lighting, weather, grain и post-processing rules.
- Визуална прогресия на офиса.
- Achievements и extras.

## Resolution protocol

При решение: добави го в `DECISIONS.md`, актуализирай/затвори въпроса, промени засегнатите system docs и не използвай `VALIDATED` без test evidence.
