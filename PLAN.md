# World of War-in-locked: Plan

An RPG with dynamic growth. Built in C# so it can move to Unity later.

## Run it
`dotnet run --project src/WarInLocked.Game` (needs the .NET 8 SDK).

## Structure
- `src/WarInLocked.Core`: game rules (no engine code, Unity-ready). `src/WarInLocked.Game`: front end with `Screens/`.
- `tests/`: tests on the rules library.
- Open questions live in `QUESTIONS.md`; work items are GitHub issues.

## Milestone 1: Start a new game
| Issue | What | Owner | Status |
|---|---|---|---|
| #1 | Main menu: title, New Game, Exit | Mapmaker | Polished (912418e), awaiting Dyllan's test |
| #2 | Character creator: name, race (Human/Elf/Dwarf), stat distribution | Developer | Built (abf6f78, 30 tests), awaiting Dyllan's test |
| #3 | Stat distributions (each adds +15) | Developer | Done in rules |

## Milestone 2: The Forest of Beginningss
| Issue | What | Status |
|---|---|---|
| #4 | Opening story text after character creation | To do |
| #5 | First map: forest, wandering Slime placeholders, one NPC, no combat | Built by Mapmaker (35e93ed): 50x24 text map, 12 slimes, WASD/arrows |
| #6 | NPC interaction: Talk option | To do |
| #7 | Colonel Powers dialogue: 3 options, 2 and 3 loop back, 1 continues | To do |

Flow: Main menu > Character creator > Opening story > Forest of Beginnings > talk to Colonel Powers.

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

## Milestone 3: First quest and gear (issues #8 to #10)

Milestone 2 (#4, #6, #7) landed in 1252559 and awaits Dyllan's test.

| # | Issue | What it needs |
|---|---|---|
| 8 | Tutorial Quest "Slice and slime" | After Colonel Powers' option 1 he gives a Practice Sword, then a quest prompt (Accept Yes/No). Kill 5 slimes (counter 0/5). Reward 50 XP and the Slash ability. No failure condition. Needs basic combat with slimes, XP and abilities. |
| 9 | Inventory | Base inventory with 10 slots. The Practice Sword lands here. |
| 10 | Equipment | Slots: Head, Shoulder, Chest, Legs, Feet (armour), Main Hand (weapon), Off Hand (weapon or shield), 3 Accessory. |

Suggested order: #9 inventory, then #10 equipment, then #8 quest (it depends on both, plus combat).

Open questions for #8: How does slime combat work (turn-based screen or real-time on the map)? What does Slash do, and where does XP go (levels)? What happens if the player says No to the quest? Do slimes respawn?

### Added 31 Oct: #11 Combat and #12 Player UI (levelling is on hold, per Dyllan)

| # | Issue | What it needs |
|---|---|---|
| 11 | Combat | Real-time hotkey combat on the map. A basic attack command deals the weapon's base damage, scaled by stats. A hotbar holds abilities (Slash goes here). Hit slimes show damage with a flicker or a shrinking health bar over their heads. Replaces #8's Space-to-defeat placeholder. |
| 12 | Player UI | Top right: name, level, health bar and energy bar. Health is 20 per Constitution point and energy is 5 per Wisdom point (Warrior 260/35, Merchant 180/45, Mystic 100/65). |

Character menu (#9/#10 screen) is in 6686abc, opened with I; Developer is doing the Core rules and #8.

Open questions: Which stats scale weapon damage? What does Slash do and cost (energy)? For health and energy, does it round down per full 5 points (Warrior Con 13 gives 200 HP, Mystic Con 5 gives 100)? Do slimes fight back?

### Status 31 Oct, 22:00
#8 to #11 rules are on main up to 5a53e37 (119 tests), and the I menu uses real items (25d4cdb). Developer is drawing #12's bars, the hotbar and the slime health bars in ForestScreen. Placeholder numbers (hit 10 + half Strength, slime 30 HP, Slash 1.5x for 10 energy, energy regenerates 2 per second, health and energy count full blocks of 5) are in QUESTIONS.md for Dyllan. Not yet seen running: Slash in game. Slimes don't fight back yet.
