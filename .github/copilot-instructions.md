# DetectiveGame инструкции за Copilot

Canonical repository инструкциите са в `AGENTS.md`. Прилагай ги към всички suggestions и edits.

Преди implementation прочети `docs/00_INDEX.md`, `docs/foundation/DECISIONS.md` и съответната system specification. Не превръщай proposal или open question в код. Пази принципа: играта предоставя информацията, а играчът прави дедукцията.

За Unity/C#: малки cohesive `MonoBehaviour` класове; serialized compatibility; без ръчна редакция на `Assets/_Game/Input/DetectiveGameInput.cs`; без speculative architecture/networking; отделяй facts, statements и interpretations; винаги давай verification steps и актуализирай документацията.

