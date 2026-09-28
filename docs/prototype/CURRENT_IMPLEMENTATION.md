# Текуща implementation снимка

**Status:** `IMPLEMENTED` snapshot, не design approval.  
**Repository inspected:** 2026-09-28.  
**Evidence:** static inspection; runtime не се твърди без изрично отбелязване.

## Project baseline

- Unity Editor: `6000.6.3f1`.
- URP: `17.6.0`.
- Input System: `1.20.0`.
- UGUI: `2.6.0`.
- Test Framework: `1.8.0`.
- Active build scene: `Assets/_Game/Scenes/DetectiveOffice.unity`.
- Build settings съдържа disabled stale reference към изтрития `Assets/Scenes/SampleScene.unity`.
- Gameplay code: `Assets/_Game/Scripts/`.
- 20 authored C# files, приблизително 1900 реда към датата на проверката.
- Няма namespaces или assembly definitions; кодът е в `Assembly-CSharp`.
- Working tree съдържа значителни user changes спрямо initial commit; те трябва да се пазят.

## Implemented input

Source: `Assets/_Game/Input/DetectiveGameInput.inputactions`.

| Action | Binding |
|---|---|
| Move | WASD |
| Look | Pointer delta |
| Interact | E |

Няма control schemes, gamepad bindings, pause/back action или отделни gameplay/UI action maps.

`DetectiveGameInput.cs` е generated и не се редактира ръчно.

## Player и interaction

- `FirstPersonController`: `CharacterController`, mouse look, pitch clamp и simple gravity.
- `PlayerInputReader`: wrapper около generated input class.
- `PlayerInteraction`: camera-forward raycast до 3 units при Interact.
- Проверява `IInteractable` върху директно ударения collider GameObject, без parent lookup.
- `IInteractable` има само `Interact()`.
- Няма interaction prompt, crosshair state, focus feedback, priority или rebinding UI.
- `DocumentInteractable` се reparent-ва към camera, спира movement и се връща при следващ interact.
- `TestInteractable` само log-ва и се завърта; prototype-only.

## Interaction modes

Computer, phone и board имат собствени active-mode flags. Те disable-ват player movement/interaction, unlock/show cursor, слушат UI exit event и възстановяват world state. Няма central mode coordinator.

## Computer

Implemented: `ComputerInteractable`, `ComputerPowerController`, `ComputerUIController`, `ComputerPowerState`.

Power states: `Off`, `Booting`, `On`, `Sleeping`. Има boot, wake, sleep, restart, shutdown, power menu, desktop, application windows и single-window behavior. Default code timing: 3s boot и 1.5s shutdown.

### Email

- `EmailData`, `EmailEntryUI`, `EmailUIController` са implemented и scene-wired.
- Inbox entries се instantiate-ват от prefab веднъж.
- Сцената съдържа два placeholder emails: preliminary examination и parking receipt.

### Case Files

- `CaseData`, `CaseEntryUI`, `CaseFilesUIController` са implemented.
- За разлика от стария `PROJECT_OVERVIEW.md`, текущата сцена вече wire-ва controller, window, content и prefab.
- Има два placeholder cases: open homicide и archived warehouse fire.
- Overview/People/Records panel switching е implemented; content depth е placeholder.

### Database, Photos, Forensics

- Има icons, windows, content objects и close controls.
- `ComputerUIController` ги отваря/затваря.
- Нямат substantive domain behavior или scalable data model.

## Phone

- `PhoneInteractable` и `PhoneUIController` са wired.
- Open, contacts, start call, hang up и exit работят на panel ниво.
- Content е direct serialized strings.
- Текущите values са placeholder и не са canon.
- Няма call routing, branching dialogue, evidence-dependent questions, audio calls или persistent state.

## Evidence board

- `EvidenceBoardInteractable` и `EvidenceBoardUIController` са wired.
- Board list/detail view с избор по index.
- Data е private nested serializable class в UI controller-а.
- Три placeholder entries: victim photo, parking receipt, witness statement.
- Няма placing, moving, linking, filtering, timeline или player-authored deductions.

## Scene/content

- Един office scene с primitive placeholder geometry.
- Player/Main Camera, EventSystem, Environment, Furniture, Computer, Phone, EvidenceBoard, Document_Test, TestInteractable и Canvas UI.
- `Art`, `Audio`, `Data` и `UI` съдържат малко или никакво final authored content.
- Email и case row prefabs са в `Assets/_Game/Prefabs/Computer`.

## Not implemented

- Crime scenes, person inspection, body examination.
- Photography workflow.
- Shared evidence/knowledge domain model.
- Statement reliability/contradiction logic.
- Timeline/deduction mechanics.
- Accusation/consequences.
- Save/load и case authoring pipeline.
- Authored automated tests.
- Gamepad/accessibility settings.
- Final art/audio/narrative content.

## Verification required

- Unity `6000.6.3f1`: zero compile errors.
- Play Mode: всички P0 checks от `ACCEPTANCE_TESTS.md`.
- Console inspection по време на mode transitions.
- UI readability при target Windows resolution.
- Human playtest преди experience validation.

