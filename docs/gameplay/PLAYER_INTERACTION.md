# Player interaction

**Status:** basic raycast interaction е `IMPLEMENTED`; final interaction language е `OPEN` по Q-002/Q-004.

## Player experience

Играчът поглежда meaningful object, получава restrained feedback, използва един consistent input и влиза в appropriate focused state. Общата граматика трябва да важи за office и crime-scene objects, освен при умишлено различно physical action.

## Current rules

- Camera-forward raycast при `E`.
- Максимална дистанция 3 units.
- Direct hit collider GameObject трябва да има `IInteractable`.
- Computer, phone и board отварят cursor-driven UI modes.
- Document inspection мести physical object пред camera.

## Proposed responsibilities

- **Input reader:** semantic actions, без target decisions.
- **Target resolver:** eligible interactables, distance/layer/focus rules.
- **Interaction contract:** availability, optional prompt и invoked action.
- **Mode coordinator:** exclusive focused mode и control/cursor restoration, ако complexity го оправдае.
- **Focus feedback:** показва interactability, не clue importance.

## Must happen

- Feedback показва usability, не narrative importance.
- Всеки focused mode има consistent exit.
- World input не изтича в cursor UI.
- Re-entry работи след всеки supported exit.
- Invalid target не trap-ва controls.

## Must not happen

- Important clues glow, а останалото очевидно не е важно.
- Prompt като „Inspect murder weapon“ издава conclusion.
- Всеки interactable измисля собствен несъвместим cursor/control модел.
- Multiple focused modes са active едновременно.

## Open

Crosshair/outline/text feedback; universal exit input; parent lookup; hold/click/rotate/zoom; gamepad/accessibility.

