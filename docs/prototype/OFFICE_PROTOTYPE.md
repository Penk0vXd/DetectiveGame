# Detective Office Prototype — specification

**Status:** prototype-ът е `DECIDED`; част от success criteria са `OPEN`.  
**Цел на фазата:** да докаже, че office-ът е приятно и разбираемо investigation пространство, преди да изграждаме пълен case.

## Prototype question

Може ли играчът удобно да обитава малък first-person detective office, да преминава между физически investigation tools, да получава информация от няколко source-а и да усеща, че стаята подпомага detective work, а не е сбор от несвързани менюта?

## Target player flow

```text
Влизане в office
→ ориентация и движение
→ откриване на investigation tool
→ използване на computer
→ прочитане на case-related email или case file
→ безопасно напускане на computer mode
→ phone call със свързана информация
→ inspection на document/photo
→ преглед на evidence board
→ самостоятелно разпознаване, че информацията принадлежи към едно investigation
```

Точното съдържание и редът са `OPEN` по Q-003. Това е test shape, не final case design.

## Included systems

- First-person movement и mouse look.
- Raycast interaction с consistent input.
- World mode и focused interaction modes.
- Computer power/desktop interaction.
- Email и Case Files.
- Placeholder Database, Photos и Forensics windows.
- Phone и minimal call state.
- Physical document inspection.
- Evidence-board overview/detail.
- Cursor capture/release и control restoration.

## Player experience requirements

- Играчът разбира как да approach, enter, use и exit всеки tool.
- Focused mode има ясен и надежден exit.
- Текстът е четим при target resolution.
- Spatial orientation се запазва при преминаване между world и UI.
- Поне два source-а дават свързана информация, без UI да обявява conclusion-а.
- Progress не зависи от pixel hunting.

## Rules

1. Само един focused mode може да контролира input-а.
2. Focused UI mode спира world movement/look и world interaction.
3. Cursor се отключва и показва, когато UI го изисква.
4. Exit възстановява world-control state точно веднъж.
5. Document inspection връща документа на original transform.
6. Re-entry работи след close, sleep, restart, shutdown, call end или detail view.
7. Placeholder content не става narrative canon по подразбиране.

## Edge cases

- Повтарящ се interact по време на boot/shutdown.
- Disable/destroy на interactable в active focused mode.
- Exit от nested app или evidence detail.
- Re-entry след sleep, restart и shutdown.
- Phone close преди/по време/след call.
- Document close, когато ray вече не сочи към документа.
- Missing Inspector references.
- UI root disabled externally, докато е marked open.
- Rapid input, водещ до двойно enter/exit.

## Proposed minimal content

- Един active case identifier.
- Един email с тесен factual report.
- Един call с claim или follow-up.
- Един physical document/photo със свързан детайл.
- До три board entries.
- Едно contradiction, което играчът забелязва без system label.

Този content set е `PROPOSED`, докато Q-003 не бъде решен.

## Must happen

- Всеки tool участва в coherent investigation flow.
- Mode transitions са stable и reversible.
- Prototype-ът произвежда evidence за usability и detective feeling.
- Playtest отделя „разбрах controls“ от „направих deduction“.

## Must not happen

- Разрастване в full crime scene или complete case.
- Final deduction/accusation architecture преди design decisions.
- Всички desktop icons да станат required само защото съществуват във vision-а.
- Networking, open world, procedural systems или production save infrastructure.
- Visual polish да замени prototype question-а.

## Exit criteria

Functional completion изисква всички P0 checks в `ACCEPTANCE_TESTS.md`. Experience validation изисква реално playtest evidence за Q-001. Това са отделни gates.

