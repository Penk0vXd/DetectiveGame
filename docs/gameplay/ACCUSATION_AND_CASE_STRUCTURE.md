# Accusation и case structure

**Status:** wrong conclusion е `DECIDED`; submission format и consequences са `PROPOSED/OPEN`.

## Candidate case contents

Victim, suspects/witnesses, locations, timeline, evidence sources, motives/opportunities, claims/contradictions, optional evidence, red herrings, theories, conclusion и consequences.

## Candidate accusation fields

Accused person, motive, method/weapon, location, timeline/event sequence и supporting evidence. Точните fields и mandatory status са `OPEN`.

## Wrong conclusions

Wrong accusation не се блокира само защото game знае canonical answer. Consequences могат да включват innocent accused, offender free, changed relationships или future information. Те са `PROPOSED` и имат high scope cost.

## Case authoring invariants

- Canonical truth е internally consistent.
- Required conclusions имат fair support.
- Alternative explanations са plausible по ясна причина.
- Red herrings имат in-world cause.
- Mandatory/optional/missable/contradictory content е explicit.
- Validation проверява correct и plausible wrong paths.

## Must happen

- Player deliberately submits conclusion.
- Submission basis се пази, когато design-ът го изисква.
- Incorrect conclusions са възможни.
- Consequences съответстват на actual choice.

## Must not happen

- „Cannot accuse“ само защото person не е canonical killer.
- Killer reveal след all clues collected.
- Surprise mandatory evidence след accusation.
- Massive branching преди validated single-case loop.

Предпочитаме малък брой добре направени cases. Числото 5–10 остава `PROPOSED`.

