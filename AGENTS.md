# DetectiveGame — основни инструкции за AI

Този файл е задължителната входна точка за всеки AI агент, който работи по код, дизайн, research, review, testing или документация в това repository.

## Мисия

Изграждаме оригинална 3D first-person detective / murder-mystery игра с Unity и C#. Играта предоставя информацията, но играчът — не системата — прави дедукцията.

Търсеното усещане е:

> „Чакай… този човек каза, че е бил вкъщи в 22:00, но това доказателство го поставя другаде.“

Не го заменяй с:

> „Quest updated: confront the liar.“

## Задължителен ред за четене

Преди промяна:

1. Прочети целия `AGENTS.md`.
2. Прочети `docs/00_INDEX.md`.
3. Прочети `docs/foundation/DECISIONS.md` и `docs/foundation/OPEN_QUESTIONS.md`.
4. Прочети документите за конкретната задача, посочени от индекса.
5. Провери реалния код, сцени, prefabs, assets и settings. Документацията не доказва implementation.
6. Провери `git status` и запази несвързаните потребителски промени.

За Office Prototype задължително прочети и:

- `docs/prototype/OFFICE_PROTOTYPE.md`
- `docs/prototype/CURRENT_IMPLEMENTATION.md`
- `docs/prototype/ACCEPTANCE_TESTS.md`

## Език

- Основният език за документация и комуникация е български.
- Оставяй на английски имена на файлове, C#/Unity identifiers, API имена и утвърдени технически термини, когато преводът би създал неяснота.
- Статусите `IMPLEMENTED`, `DECIDED`, `VALIDATED`, `PROPOSED`, `OPEN QUESTION`, `NEEDS RESEARCH`, `DEFERRED`, `REJECTED` и `REVISIT` остават на английски и се използват точно.

## Модел на истината

- **IMPLEMENTED:** реално съществува в текущото repository. Кодът, сцените, prefabs, settings и провереното runtime поведение са authoritative.
- **DECIDED:** одобрено product или architecture решение. Canonical source е `docs/foundation/DECISIONS.md`.
- **PROPOSED:** възможна посока, която не е одобрена.
- **OPEN QUESTION:** изисква решение от потребителя, преди да бъде заключена важна механика или architecture.
- **NEEDS RESEARCH:** изисква актуални надеждни източници.
- **DEFERRED:** умишлено е оставено за по-късна фаза.

Ако implementation и документация си противоречат, докладвай противоречието. Не променяй тихо `DECIDED` правило, за да пасне на случаен код, и не твърди, че планирано поведение вече е реализирано.

## Неподлежащи на самоволна промяна решения

- 3D first-person detective / murder-mystery игра.
- Unity и C#; Windows PC е първата платформа.
- Single-player first.
- Без open world за началната версия.
- Multiplayer и networking не са текущ приоритет.
- Физическият detective office е централен hub, а не меню.
- Играта включва computer, phone, documents, photos, statements, evidence, crime scenes, inspection на хора, body examination, board, timeline, deduction и accusation/case conclusion.
- Statement е claim, а не автоматично факт.
- Fact и interpretation са отделни понятия.
- Събирането на всички clues не разкрива автоматично убиеца.
- Играчът може да стигне до грешно заключение.
- Работим чрез малки prototypes; първият е Detective Office Prototype.
- Проектът служи и за учене на C# и Unity. Не заменяй обучението с непоискани огромни готови решения.

## Проектиране на система

За всяка нова система работи в реда:

1. Player Experience
2. Rules
3. Edge Cases
4. Data Model
5. System Responsibilities
6. Architecture
7. Implementation
8. Testing

Не започвай с код, ако player experience или основните правила са нерешени.

За нова механика описвай: Idea, Player Experience, Why it exists, Risks, Technical Cost, Minimal Prototype и Decision Status.

## Scope discipline

Всеки feature трябва да подобрява detective gameplay, производствената реалистичност, необходимата usability или учебната стойност. „Звучи интересно“ не е достатъчно.

Отхвърляй или отлагай преждевременно:

- open-world simulation и procedural city;
- десетки плитки случаи;
- advanced autonomous NPC schedules;
- vehicles като traversal system;
- multiplayer/networking;
- massive branching narrative;
- production architecture за хипотетични нужди;
- design pattern, abstraction, package или service без текущ проблем.

Приоритети: `P0` core gameplay, `P1` architecture, `P2` production, `P3` polish/later.

## Detective-design инварианти

- Информацията може да е непълна, подвеждаща, нерелевантна или двусмислена.
- Странен визуален детайл не е автоматично evidence.
- Statement пази какво твърди източникът, а не каква е обективната истина.
- Timestamp доказва само това, което надеждно записва източникът.
- UI не обявява кой лъже, освен при изрично одобрено действие за проверка.
- Interaction feedback показва usability, не narrative importance.
- Red herrings са правдоподобни и честни, а не произволен шум.
- Грешните заключения произлизат от fallible reasoning, не от нечестно скрита задължителна информация.
- Office, computer, phone, board и timeline са investigation tools, не декоративни менюта.

## Coding rules

- Разграничавай ясно `PROTOTYPE CODE` от `PRODUCTION DIRECTION`.
- Предпочитай малки отговорности пред giant `MonoBehaviour` класове.
- Използвай composition и interfaces само когато решават реален проблем.
- Не hardcode-вай разрастващо се case content в gameplay-control scripts.
- Отделяй domain data/state от presentation, когато активната система го изисква.
- Пази serialized field compatibility или осигури migration.
- Не редактирай ръчно генерирания `Assets/_Game/Input/DetectiveGameInput.cs`; редактирай `.inputactions` asset-а и го регенерирай.
- Третирай сцени, prefabs, `.meta` файлове и serialized references като свързани assets.
- Не добавяй package без purpose, version, ownership и removal cost.
- Не добавяй networking architecture.
- Не commit-вай secrets, builds, `Library`, `Temp`, `Logs`, `obj` или IDE caches.

## Текущи prototype факти

- Активна сцена: `Assets/_Game/Scenes/DetectiveOffice.unity`.
- Gameplay scripts: `Assets/_Game/Scripts/`.
- Input asset: `Assets/_Game/Input/DetectiveGameInput.inputactions`.
- Текущи controls: `WASD`, pointer look и `E` за interact.
- Сцената съдържа movement, raycast interaction, document inspection, computer, email, case files, placeholder database/photos/forensics windows, phone и evidence board.
- Текущото съдържание е placeholder и не е narrative canon.
- Няма namespaces или assembly definitions; това е `IMPLEMENTED`, не финална architecture препоръка.

## Протокол за промени

Преди implementation:

- Определи статуса: fix на `IMPLEMENTED`, implementation на `DECIDED`, prototype на `PROPOSED` или research по `OPEN QUESTION`.
- Посочи засегнатите файлове и acceptance evidence.
- Ако липсва P0/P1 решение, спри преди да го заключиш в architecture и поискай решение.

По време на implementation:

- Направи най-малката цялостна промяна, която изпълнява одобреното поведение.
- Запази несвързаните промени.
- Не пренаписвай сцени/prefabs изцяло, ако е възможна фокусирана промяна.
- Не разширявай тихо scope-а.

След implementation:

- Провери compilation и runtime behavior според риска.
- Отбележи честно непровереното в Unity Editor.
- Актуализирай system документа и `CURRENT_IMPLEMENTATION.md`.
- Добавяй решение в `DECISIONS.md` само след одобрение.
- Докладвай променени файлове, evidence, рискове и нужни manual Unity стъпки.

## Testing

- Compilation не доказва scene wiring или gameplay behavior.
- Scene reference не доказва runtime коректност.
- Проверявай world control, cursor state, UI state, re-entry и всички exit paths.
- Не твърди playtest success без реално наблюдение.
- Използвай `docs/prototype/ACCEPTANCE_TESTS.md`.

## Research

Използвай актуален web research за Unity versions, packages, assets, platform rules, съвременни игри и market информация. Предпочитай official/primary sources.

За forensic, autopsy, police, evidence или legal procedure разделяй `REAL WORLD` от `GAME ABSTRACTION`. Research output винаги разделя facts, assumptions, recommendations и decisions for this game.

## Обучение

При нова концепция:

1. Обясни защо съществува.
2. Дай ограничена задача или насока.
3. Остави потребителя първо да опита, когато е практично.
4. Прегледай опита с конкретни evidence.
5. Дай пълно решение само при поискване или изрично разрешена implementation задача.

## Документация

- Документацията е AI-first: explicit status, точна терминология, paths, invariants, inputs, outputs, failure modes и verification.
- Поддържай един canonical source за всеки факт; другите файлове сочат към него.
- Не копирай целия project prompt във всеки model adapter.
- Датирай implementation snapshots и research.
- Всеки system документ описва какво трябва и какво не трябва да прави системата.
- Актуализирай `docs/00_INDEX.md` при добавяне или местене на документ.

## Входни точки за AI модели

- Codex и общи coding agents: `AGENTS.md`.
- Claude Code: `CLAUDE.md`, след това `AGENTS.md`.
- Gemini: `GEMINI.md`, след това `AGENTS.md`.
- Други модели: `AI_CONTEXT.md`, след това `AGENTS.md`.
- `CODEX.md` е кратък human-visible adapter; `AGENTS.md` остава authoritative.

