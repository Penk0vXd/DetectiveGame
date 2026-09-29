# Computer system

**Status:** production-oriented prototype C# foundation, application UI hierarchies, prefabs и Inspector wiring са `IMPLEMENTED`; runtime validation остава pending. Final production behavior остава `PROPOSED/OPEN`.
**Последна code проверка:** 2026-09-29.

## Purpose

Computer е diegetic investigation tool за получаване, търсене и review на информация, а не decorative desktop simulation.

## Current implementation

- Physical interaction и focused computer mode.
- `Off`, `Booting`, `On`, `Sleeping`.
- Boot, sleep, wake, restart, shutdown и power menu.
- Single-window application behavior.
- Functional Email и Case Files Overview.
- `CaseData` поддържа official People и Records data; controllers и generated entry callbacks са implemented.
- Police Database v1 има case-insensitive `FullName` search, generated results, selection, details и clear.
- Photos v1 има generated gallery/list, Sprite preview и metadata.
- Forensics v1 има generated reports list, selection и report details.
- `ComputerUIController` използва отделни app controllers и пази старите placeholder window references като backward-compatible fallback.
- Всички application controllers, generated-list prefabs, UI references и button bindings са scene-wired чрез `ComputerUISetupTool`.
- Computer shell и application windows използват `Police Desk 98` visual style: teal desktop, classic gray square panels, navy title bars, hard outlines, beveled buttons, големи uppercase labels и `PoliceTerminal` TMP font.

## Candidate applications

| App | Investigation value | Current state |
|---|---|---|
| Email | Reports/messages от identifiable sources | Basic implemented |
| Case Files | Case summary, officially linked people и records | C# и scene wiring implemented; runtime validation pending |
| Database | Search по player-entered FullName | C# и scene wiring implemented; runtime validation pending |
| Photos | Photo list, preview и metadata | C# и scene wiring implemented; runtime validation pending |
| Forensics | Laboratory/autopsy reports без automatic interpretation | C# и scene wiring implemented; runtime validation pending |

## Rules

- Entry пази source, date/time и case association, когато са relevant.
- Reports дават observations/qualified findings, не guaranteed conclusions.
- Search изисква meaningful input, не „reveal next clue“.
- Locked content не трябва да издава, че липсващото е важно.

## Must happen

- Reliable enter/exit.
- Legible и source-attributed content.
- Distinct purpose за applications.
- Cross-tool information sequence.

## Must not happen

- Full operating-system simulation без gameplay value.
- Apps само за запълване на desktop.
- Automatic suspect cross-reference.
- Player interpretation като official forensic fact.
- Boot/power delay без доказана pacing стойност.

## Data и ownership

- Всички v1 application records са `[System.Serializable]` classes в serialized `List<T>` fields.
- Data classes пазят data и read-only accessors; не управляват UI или progression.
- Entry UI scripts представят един generated row и изпращат typed selection callback.
- Всеки application controller управлява само своя window, lists, selection и details.
- `ComputerUIController` управлява shell state и exclusive application switching; не съдържа application data.
- Няма ScriptableObject repository, central database service, singleton, event bus или automatic cross-reference.

## Failure modes и guards

- Missing window/list/prefab references прекратяват операцията с еднократен warning, вместо да причинят `NullReferenceException`.
- Null data elements се пропускат.
- Static lists се build-ват веднъж; Case People/Records и Database results изчистват старите generated entries преди rebuild.
- Database empty query показва `Въведете пълно име`; zero matches показва `Няма намерени записи`.
- Missing Photo Sprite е допустим и скрива само Image component-а; metadata остава usable.

## Verification

- `STATIC/COMPILATION` на 2026-09-29: `Assembly-CSharp` build с Unity `6000.6.3f1` references — 0 errors, 0 warnings.
- `EDITOR` на 2026-09-29: Unity batch setup създаде hierarchy, петте липсващи prefabs, test data, non-null controller references и точно по един persistent listener за всеки documented button; tool validation завърши успешно.
- `EDITOR VISUAL` на 2026-09-29: повторно изпълнение на setup tool-а приложи `Police Desk 98` styling и създаде локалния `PoliceTerminal SDF` font asset; batch process завърши с code `0`.
- `EDITOR LANGUAGE` на 2026-09-29: Computer labels и development records са на български; `PoliceTerminal SDF` е свързан с dynamic Cyrillic fallback; batch compilation завърши с 0 errors и 0 warnings.
- `RUNTIME`: pending Play Mode проверка по `COMPUTER_SETUP_GUIDE.md`.
- Required runtime flows: `docs/prototype/ACCEPTANCE_TESTS.md` и full checklist в setup guide-а.

## Open

Required prototype apps; value на power simulation; Database search freedom; unread states; data flow към board/timeline/notes.
