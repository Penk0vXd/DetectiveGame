# Detective board, timeline и deduction

**Status:** system presence е `DECIDED`; interaction model е `OPEN`; текущият board е само `IMPLEMENTED` detail viewer.

## Purpose

- **Board:** външно представя case elements и relationships.
- **Timeline:** events, claims, uncertainty ranges и conflicts във времето.
- **Deduction:** позволява player-formed conclusions без automatic correct answer.

## Candidate approaches

### Free linking

Висока agency, но links може да нямат mechanical meaning и controller UX е сложен.

### Predefined propositions

Ясна validation, но UI може да издава intended reasoning.

### Hybrid

Free organization плюс authored formal deductions. Балансиран candidate, но има две interaction grammars.

Нито един approach не е `DECIDED`.

## Timeline principles

- Entry пази source и uncertainty.
- Exact time и time range са различни.
- Conflicting entries могат да coexist.
- „Watch stopped at 22:17“ не означава automatic „death at 22:17“.
- Unknown gaps не диктуват автоматично solution.

## Must happen

- Видима basis на relationship/timeline entry.
- Representation на conflicts.
- Deliberate formal conclusion.
- Thinking support без solution reveal.
- Plausible wrong theories могат да съществуват.

## Must not happen

- Connect correct cards → automatic truth unlocked.
- Hide incorrect links преди accusation.
- Linear evidence progress meter.
- Visual emphasis върху единствената correct connection.
- Final deduction engine по време на Office Prototype.

