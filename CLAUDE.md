# Входна точка за Claude Code

Прочети изцяло `AGENTS.md`, преди да предприемеш действие. Това е основният и canonical файл с инструкции за repository-то.

След това прочети:

1. `docs/00_INDEX.md`
2. `docs/foundation/DECISIONS.md`
3. `docs/foundation/OPEN_QUESTIONS.md`
4. документите за конкретната задача, посочени от индекса
5. реалния код, сцените, prefabs и settings, засегнати от задачата

Правила специално за Claude:

- Описвай `IMPLEMENTED` поведението според repository evidence, а не според паметта от разговора.
- Не превръщай `PROPOSED` механики в `DECIDED`.
- Не вземай нерешени P0 game-design или P1 architecture решения вместо потребителя.
- Запазвай несвързаните промени в working tree.
- Не редактирай ръчно генерирания `Assets/_Game/Input/DetectiveGameInput.cs`.
- След промяна на поведение актуализирай implementation snapshot-а и съответната system документация.
- Отделяй провереното в Unity Editor от изводите, направени само чрез static inspection.

За специализираните AI роли прочети `docs/ai/ROLE_PROMPTS.md`.
