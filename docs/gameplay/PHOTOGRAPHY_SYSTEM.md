# Photography system

**Status:** feature presence е `DECIDED` на vision ниво; player camera и zoom discoveries са `PROPOSED`; не е implemented.

## Purpose

Photography позволява deliberate preservation на viewpoint, later review и свързване на crime-scene observation с office analysis.

## Candidate flow

```text
Camera → frame subject → capture → case record
→ computer/board review → zoom/pan → player notices detail
```

## Approaches

- **Free capture:** high agency, high storage/save/rendering cost.
- **Authored capture points:** lower cost, риск от checklist markers.
- **Hybrid:** free-looking camera с validated subject/composition.

Няма `DECIDED` approach.

## Risks

Hundreds of photos, unclear framing, fake zoom details, technical storage cost и duplication на ordinary inspection.

## Must happen

- Photo е deliberate investigative action.
- Пази case/time/location/source context, когато е relevant.
- Zoom detail реално присъства в captured image.
- Review позволява comparison без automatic interpretation.

## Must not happen

- „Photograph this“ marker върху всяко важно нещо.
- Shutter автоматично добавя conclusion.
- Detail се появява след zoom, въпреки че не е в source image.
- Unrestricted capture преди storage/performance/save requirements.

Не implement-вай photography без един конкретен test question, scene, meaningful photo и success criterion.
