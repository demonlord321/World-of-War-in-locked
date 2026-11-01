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

### Status 31 Oct, 22:07
#8 to #12 are all on main (HUD, hotbar and slime bars in 94a6829; health 20 per Constitution and energy 5 per Wisdom in 8f5abb9). Everything awaits Dyllan's test. Issues are still open.

### Added 1 Nov: #13 Level up (replaces "levelling on hold")
XP bar under the energy bar. XP cost to the next level grows by a step: +50 per level up to 5 (50, 100, 150, 200), then +100 per level after 5, the step doubling to +200 from level 10 and then doubling every 10 levels (my reading: +400 from 20, +800 from 30). Each level gives 3 free stat points to assign, and every 5th level gives 5.

Open questions: Is that reading of the curve right (5 to 6 costs 300, 9 to 10 costs 700, 10 to 11 costs 900)? Does a 5th level give 5 points instead of 3, or 3 + 5 = 8? Do health and energy update when points go into Constitution and Wisdom (yes, presumably)? Where are points assigned (the I menu?) Is there a level cap?
Decided 1 Nov: every 5th level gives 5 points instead of 3 (Dyllan). Curve as read above (confirmed by the team, built in 5c7611f, 149 tests). No level cap for now. Points can be taken back only while the I menu is open (Mapmaker drawing the XP bar, level-up banner and +/− buttons).

### Added 1 Nov: #14 Physical Damage, #15 Physical Defense, #16 Magical Damage
Order (Dyllan): #14 and #16 first; #15 is to be discussed.
- #14: unarmed damage = 10 x Strength modifier. The modifier is +0.1 per Strength point and +1 at each milestone (10, 25, 50), so 9 Strength gives 0.9. Equipment adds on top. Replaces the 10 + half Strength placeholder.
- #16: magic damage works the same way but uses Intelligence.
- #15: defense works the same way but uses Constitution. Its design is still to be discussed with Dyllan.
Decided 1 Nov: a milestone point adds 1 on top of its usual 0.1, so modifier = 0.1 x stat + 1 per milestone reached (9 gives 0.9, 10 gives 2.0, 25 gives 4.5, 50 gives 8.0).
Status 1 Nov, 08:23: #14 and #16 rules in 6703ac8 (179 tests), Combat box in the I menu in ad4fefd. Placeholders: Practice Sword +5 before the multiply, slimes 30 HP. #15 under discussion: health = base x Constitution modifier (base 10 or 100?), and whether defense is also a separate damage reduction.
Decided 1 Nov (#15): max health = 100 x Constitution modifier (Mystic 50, Merchant 90, Warrior 230). Energy stays 5 per Wisdom point for now.

### Added 1 Nov: #17 Energy, #18 Modifiers on the stat page
- #17: max energy = base x Wisdom modifier. Base not given yet (Warrior Wis 7, Merchant 9, Mystic 13 give 0.7 / 0.9 / 2.3).
- #18: the I menu shows each stat with its modifier in brackets, e.g. "Str 10 (2)", for all 8 stats, all on the same curve. Mapmaker's screen, using Core's modifier.
Decided 1 Nov (#17): energy base 50 (Warrior 35, Merchant 45, Mystic 115). Note: Mystic energy 115 is above Mystic health 50.
Decided 1 Nov: Mystics having more energy than health is intended (casters don't tank). Energy built in 0dc13a7 (201 tests).
Note (Dyllan, 1 Nov): Warrior, Mystic and Merchant are starting stat templates, not classes. Real classes come later, and builds will diverge as players assign level-up points. #18 modifiers in 7b12af7.

### Added 1 Nov: #19 Tutorial Quest 2, #20 Abilities
- #19: quest 1 now ends by returning to Colonel Powers for the rewards (a change from the current auto-reward). Then his two lines, and the quest "Slashing Practice": use Slash 5 times on enemies (0/5). Reward: Energy Ball ability and 50 XP. No failure condition.
- #20: Slash (lvl 1) damage = (10 + n) x Strength modifier, where n starts at 5 and rises by 1 per ability level. Cooldown 5 s, cost 10 energy. Abilities level up by use: 5 uses to reach level 2, and the requirement doubles each level (10, 20, ...). Needs a player screen listing abilities. Replaces the 1.5x Slash placeholder.
Open questions: Does Slash add the weapon bonus? Does its arc hit every enemy in front? Are the use counts per level (5, 10, 20) or running totals? What does Energy Ball do (Intelligence-based, cost, cooldown)? Which key opens the abilities screen?
Decided 1 Nov (#20): Slash = (base attack + n) x Strength modifier, where base attack = 10 + weapon bonus (Practice Sword +5, so 15), and n = 5 at level 1. At 10 Strength, a sword hit is 30 and Slash is 40.
Decided 1 Nov: Slash needs a weapon in the main hand and can't be used unarmed.
Status 1 Nov, 08:53: #19 and #20 rules in b919b0d (225 tests), not run in game yet. Slimes respawn after 10 s. Placeholders: Energy Ball (10 + n) x Int modifier, 15 energy, 5 s cooldown; Colonel's quest 2 hand-in lines are filler. Ability screen: Mapmaker.
Status 1 Nov, 08:57: abilities screen on K in 3ab52ec. #8 to #20 (except #15 defense beyond health) are all on main and await Dyllan's test. Known nit: the Energy Ball label spills past its hotbar slot.

### Added 1 Nov: #21 to #24
- #21 Tutorial Quest 3 "Energy Ball practice": hand in quest 2 to the Colonel, his lines, receive a Practice Staff, cast Energy Ball 5 times (0/5). Reward: 50 XP and 50 copper (first currency). No failure condition.
- #22 Weapons: Practice Sword (Physical Attack +5) and Practice Staff (Magical Attack +5), with flavour text, plus an equipment detail screen.
- #23 Spells: Energy Ball = (base magic damage + n) x Intelligence modifier, n = 5 at level 1. Cooldown 5 s, cost 5 energy (was the 15 placeholder).
- #24 Slimes: levels 1 to 3 in the forest. Lv 1 100 HP / 10 attack, Lv 2 125 / 12, Lv 3 150 / 15. Vulnerable to both physical and magic damage (other monsters may resist).
Open questions: Do slimes now attack the player, and how often? How much more damage does "vulnerable" mean? Does Energy Ball need the staff equipped? Do slimes give XP or drop copper? How is the level 1 to 3 mix spread?
