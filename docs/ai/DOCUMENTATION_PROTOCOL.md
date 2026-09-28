# AI-first documentation protocol

**Status:** active canonical documentation rules.

## Purpose

Документацията пречи на AI да измисля context, да смесва plans с implementation, да повтаря stale analysis или да взема unauthorized decisions.

## Canonical ownership

| Информация | Canonical source |
|---|---|
| Repository rules | `AGENTS.md` |
| Approved decisions | `foundation/DECISIONS.md` |
| Unresolved choices | `foundation/OPEN_QUESTIONS.md` |
| Vision | `foundation/GAME_VISION.md` |
| Implementation snapshot | repository + `prototype/CURRENT_IMPLEMENTATION.md` |
| Prototype contract | `prototype/OFFICE_PROTOTYPE.md` |
| Tests | `prototype/ACCEPTANCE_TESTS.md` |
| Gameplay behavior | relevant `gameplay/*.md` |
| Technical direction | relevant `technical/*.md` |
| Research | `production/RESEARCH_BACKLOG.md` + dated records |
| AI roles | `ai/ROLE_PROMPTS.md` |
| Narrative | `foundation/NARRATIVE_DIRECTION.md` |
| Presentation | `presentation/ART_AUDIO_UI.md` |
| Save/load | `technical/SAVE_AND_LOAD.md` |
| Authoring | `production/CONTENT_AUTHORING.md` |

## Required header

Title, `Status`, last verified/updated date когато е time-sensitive, purpose/scope.

## System document sections

Когато са relevant: Purpose, Status, Player Experience, Current Implementation, Rules, Must Happen, Must Not Happen, Edge Cases, Data/State, Responsibilities, Testing и Open Questions.

## Update matrix

| Промяна | Update |
|---|---|
| User approves choice | `DECISIONS.md`, open question, affected docs |
| Behavior changes | `CURRENT_IMPLEMENTATION.md`, system doc, tests |
| New unresolved issue | `OPEN_QUESTIONS.md` с priority/needed-by |
| New research | `RESEARCH_BACKLOG.md` |
| Completed research | dated record; decision separate |
| New document | `00_INDEX.md` |
| Validated prototype | acceptance record + status |

## Language rule

Основният език е български. Английски остават file names, code/API identifiers, status labels и established technical terms, когато това пази precision.

## Anti-duplication

Model adapters съдържат discovery instructions, не копия на целия prompt. При divergence премахни duplication.

## Staleness

Implementation snapshots и research имат дата. Runtime claims изискват runtime evidence. Known stale document получава warning и link към replacement.

## Decision integrity

Само user-approved choices влизат в `DECISIONS.md`. AI не измисля approval, rationale, date или validation.
