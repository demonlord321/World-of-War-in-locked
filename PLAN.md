# World of War-in-locked: Plan

An RPG with dynamic growth. Built in C# so it can move to Unity later.

## Structure
- `src/`: game rules in a plain C# library (netstandard2.1, no engine code), plus a light front end (MonoGame or console) for now.
- `tests/`: tests on the rules library.
- Open questions live in `QUESTIONS.md`; work items are GitHub issues.

## Milestone 1: Start a new game
| Issue | What | Owner | Status |
|---|---|---|---|
| #1 | Main menu: title, New Game, Exit | Mapmaker | To do |
| #2 | Character creator: name, race (Human/Elf/Dwarf), stat distribution | Developer | Rules in (fee5bed), screen to do |
| #3 | Stat distributions (each adds +15) | Developer | Done in rules |

## Character rules so far
All eight stats start at 8: Strength, Dexterity, Constitution, Wisdom, Intelligence, Perception, Charisma, Luck.

| Stat | Warrior | Mystic | Merchant |
|---|---|---|---|
| Strength | +5 | -1 | 0 |
| Dexterity | +5 | +1 | +1 |
| Constitution | +5 | -3 | +1 |
| Wisdom | -1 | +5 | +1 |
| Intelligence | -1 | +5 | +1 |
| Perception | +2 | +5 | +1 |
| Charisma | 0 | +3 | +5 |
| Luck | 0 | 0 | +5 |
| Total | +15 | +15 | +15 |

Race has no stat effect yet (see QUESTIONS.md).

## Not yet defined
- What "dynamic growth" means in play (how stats change over time).
- Map, movement, combat.
