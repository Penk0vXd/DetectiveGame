# C# и Unity coding standards

**Status:** active guidance.

## Principles

- Clarity, correctness и learnability пред cleverness.
- Малки explainable responsibilities.
- Explicit core gameplay flow.
- Abstraction след observed repetition.
- Обяснявай защо pattern/Unity feature е подходящ.

## Naming

- Types/methods/properties/events: PascalCase.
- Private fields: camelCase; serialized fields private когато е practical.
- Boolean names описват state/capability: `isOpen`, `canInteract`.
- Events описват occurrence/request: `ExitRequested`.

## MonoBehaviour

- Lifecycle methods координират, не съдържат giant domain algorithms.
- Cache-вай required references.
- Validate-вай serialized dependencies с contextual errors.
- Избягвай repeated scene-wide searches.
- Subscribe/unsubscribe events symmetrically.
- Enable/disable behavior е safe и idempotent.

## Serialization

- Field rename може да загуби Inspector data; migration при нужда.
- Не променяй prefab/scene-backed types лекомислено.
- Display strings не са canonical IDs.
- Direct scene content е допустимо само за tiny prototype.

## Events/interfaces

- Event за notification без ownership върху response.
- Listeners се премахват надеждно.
- Interface само при реални multiple implementations/consumer abstraction.
- Не създавай ceremonial one-method interface за всеки class.

## Errors

- Missing required setup: clear error и safe refusal.
- Optional missing content: warning или empty state.
- Никога не оставяй controls disabled след failed path.
- Не spam-вай същия log всеки frame.

## Generated code

- Не редактирай `Assets/_Game/Input/DetectiveGameInput.cs`.
- `.csproj` не е canonical Unity configuration.

## Prototype labeling

Temporary shortcut описва test question, shortcut, replacement trigger и data migration/discard plan.

