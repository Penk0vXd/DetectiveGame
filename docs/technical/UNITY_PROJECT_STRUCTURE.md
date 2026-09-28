# Unity project structure

**Status:** current paths са `IMPLEMENTED`; repository hygiene е active guidance; future structure е `PROPOSED`.

## Current authored structure

```text
Assets/_Game/
├── Art/{Materials,Models,Textures}
├── Audio/
├── Data/
├── Input/
├── Prefabs/Computer/
├── Scenes/
├── Scripts/{Computer,EvidenceBoard,Interaction,Phone,Player}
└── UI/
```

## Rules

- Authored game content е под `Assets/_Game/`.
- Third-party content е извън `_Game` или в ясно vendor място.
- Не мести Unity assets извън Editor без `.meta` files.
- Generated Input C# се regenerate-ва от `.inputactions`, не се hand-edit-ва.
- Добавяй folders когато има real content, не speculative empty hierarchy.
- Repeated UI/list elements използват prefabs.
- Scene не е permanent database за всички cases.

## Never commit

`Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/`, local builds, secrets и user caches.

## Scene policy

- Active prototype scene: `Assets/_Game/Scenes/DetectiveOffice.unity`.
- Disabled stale SampleScene build entry се чисти само като deliberate housekeeping.
- Scene changes се проверяват за missing scripts/references.
- Избягвай wholesale YAML rewrites.

Future feature-oriented folders/asmdefs се въвеждат само при proven scale. Това не е инструкция за refactor сега.

