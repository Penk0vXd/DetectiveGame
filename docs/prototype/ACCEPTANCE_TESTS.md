# Detective Office Prototype — acceptance tests

**Status:** `PROPOSED` runtime test contract; Editor setup evidence е записано, Play Mode резултатите още не са записани.

## Evidence labels

- **STATIC:** потвърдено само от файлове.
- **EDITOR:** потвърдено в Unity Editor configuration.
- **RUNTIME:** потвърдено в Play Mode/build.
- **PLAYTEST:** потвърдено чрез observed player behavior.

Не използвай `STATIC` evidence за `RUNTIME` или `PLAYTEST` claim.

## P0 functional checks

| ID | Test | Expected result | Evidence |
|---|---|---|---|
| OF-001 | Start `DetectiveOffice` | Без compile/runtime errors | RUNTIME |
| OF-002 | WASD + mouse look | Responsive movement/look; bounded pitch | RUNTIME |
| OF-003 | E върху всеки world tool | Правилният focused mode се отваря веднъж | RUNTIME |
| OF-004 | Computer от Off | Boot screen, след това desktop | RUNTIME |
| OF-005 | Exit от desktop/nested app | UI close, cursor lock/hide, restored movement/interaction | RUNTIME |
| OF-006 | Sleep/wake/restart/shutdown | Coherent states и working re-entry | RUNTIME |
| OF-007 | Email entries | Correct sender/subject/date/body | RUNTIME |
| OF-008 | Case entries | Correct Overview, People и Records data; tabs не чупят window и selection | RUNTIME |
| OF-009 | Database, Photos, Forensics | Correct search/list/selection/details; само selected app е active и може да се затвори | RUNTIME |
| OF-010 | Phone call flow | Correct panels и restored world control | RUNTIME |
| OF-011 | Всички board cards | Correct detail/back/exit | RUNTIME |
| OF-012 | Document inspection | Връща original transform и movement | RUNTIME |
| OF-013 | Rapid repeated input | Няма duplicate mode, stuck cursor или disabled controls | RUNTIME |
| OF-014 | Console след full flow | Няма unexpected exceptions/missing references | RUNTIME |
| OF-015 | Computer UI при 1920×1080 и по-малък прозорец | `Police Desk 98` стилът остава четим; title bars, buttons, lists и long text не се застъпват | RUNTIME |

## Experience tests

| ID | Наблюдение | Failure signal |
|---|---|---|
| UX-001 | Играчът открива interaction без verbal coaching | Random input или пита за key |
| UX-002 | Играчът винаги намира exit | Stuck или очаква друг control |
| UX-003 | Разбира purpose на всеки tool | Tools изглеждат като duplicate menus |
| UX-004 | Забелязва relation между два source-а | Само изчита текста без сравнение |
| UX-005 | UI не подава intended inference | Играчът повтаря system conclusion |
| UX-006 | Office traversal е purposeful | Walking се усеща само като delay |
| UX-007 | Няма pixel hunting или excessive highlighting | Missed objects или visual clutter |

## Completion record

Editor setup evidence на 2026-09-29:

- Unity `6000.6.3f1` batch execution на `ComputerUISetupTool.BuildComputerUI` завърши с exit code `0`.
- Tool validation потвърди required Computer UI objects и петте нови entry prefabs.
- Повторният batch run приложи `Police Desk 98` palette, square outlines, beveled controls и локалния `PoliceTerminal SDF` font asset без compile warnings или errors.
- Serialized inspection потвърди non-null references за новите controllers и точно по един persistent listener за всеки documented button method.
- Това не е evidence за `OF-001`–`OF-014`; те остават pending до наблюдаван Play Mode run.

```text
Date:
Unity version/build:
Tester:
Test IDs:
Evidence type:
Failures:
Console output:
Observed player actions/quotes:
Follow-up tasks:
Validation decision:
```
