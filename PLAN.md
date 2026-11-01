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
Decided 1 Nov (#24): slime HP is 50 / 75 / 100 for levels 1 / 2 / 3 (attack still 10 / 12 / 15). Placeholders: vulnerable = +25% damage taken; Energy Ball can cast without the staff. Still open: do slimes attack now?
Status 1 Nov, 09:22: #21 to #24 on main (9f4439a, 06e6467, screens in 758d031; 247 tests). Await Dyllan's test. Still open: 25% vulnerability, slimes fighting back, slime XP/copper drops, Colonel's quest 3 hand-in lines.
Decided 1 Nov: currency is copper, silver and gold. 100 copper = 1 silver, 100 silver = 1 gold (10,000 copper = 1 gold).

### Added 1 Nov: #25 to #28
- #25 Tutorial 4 "Shopping before adventure": hand in quest 3, the Colonel's two lines, then buy 1 Potion of Lesser Healing (0/1). Reward: Adventurer title, 50 XP, Adventurer's Rations.
- #26 Items: item detail screen. Potion of Lesser Healing (dull red, tastes of strawberries): heals 50 HP, 5 s cooldown after use.
- #27 Titles: a titles screen in the player interface. Adventurer: "+2 to all stats when equipped".
- #28 Shops: after accepting quest 4, talking to the Colonel offers Talk ("Stop wasting time and let's get moving.") or Shop. The shop has Buy and Sell (Sell disabled). Buy lists the potion at 25 copper, then Buy or Haggle (Haggle disabled). A bought potion goes to the inventory.
Open questions: What are Adventurer's Rations? How is a potion used (hotbar key, I menu)? Can only one title be equipped at a time? Can the potion be bought more than once? Do +2 stats count toward modifiers, health and energy?
Added 1 Nov (#26): Adventurer's Rations restore you fully when out of combat, up to level 10; after level 10 they restore half. Built so far: #25 to #28 in ffeabe4 and 978104f (295 tests). Rations built in c8de07c: one eaten per use, quest 4 gives one, they share the potion cooldown.
Status 1 Nov, 11:45 (UTC+2): #26 rations built: usable, full recovery to level 10, half after, refused in combat (5 s placeholder window) or when fully recovered. Not run in game.
Decided 1 Nov: out of combat = no damage dealt or taken for 15 s. Rations restore health and energy. Forest slimes are passive: they fight back only once attacked, hitting every 6 s (changed from 3 s, slower than the 5 s ability cooldowns). Built in fc16861 (312 tests): an angry slime stays angry until it dies, and a respawned slime is passive again. Open: what happens when the player reaches 0 health? Does the slime chase you, and does it calm down if you walk away?
Status 1 Nov, 11:45 (UTC+2): #26 rations built: usable, full recovery to level 10, half after, refused in combat (15 s window) or when fully recovered. Not run in game.
Status 1 Nov, 11:55 (UTC+2): combat window is 15 s. Slimes are passive until hit, then hit the player every 3 s for their level attack (UpdateEnemies in Core; screen must call it). Death is a placeholder (flag + event). Not run in game.
Status 1 Nov, 11:39: slime attacks every 6 s in 35062b8; combat visuals, In combat HUD and placeholder 'You have fallen' screen in ab2df59 (312 tests). Waiting on Dyllan: death/respawn rules and whether slimes chase.

### Added 1 Nov: #29 Respawn
At 0 health: 'You have fallen... The goddess has seen fit to grant you another chance.' Automatic respawn after 5 s at the bind point. One bind point at a time, set at an inn or rest spot (none exist yet). In the tutorial you respawn next to Colonel Powers. Replaces the placeholder 'You have fallen' screen.
Open: respawn with full health and energy? Any penalty (XP, copper)? Do angry slimes calm down? Does quest progress stay?
Status 1 Nov, 12:10 (UTC+2): #29 Core done: bind point, 5 s auto respawn (Character.Respawned), CalmEnemies. Placeholders: full HP/EN, no penalty. Screen must handle the Respawned event.
Status 1 Nov, 11:47: #29 rules in 9238d93 (322 tests). Placeholders: respawn with full health and energy, no penalty, slimes calm down. Advisor's point: calmed slimes should probably reset to full HP.
Decided 1 Nov (#29): respawn at full health and energy. Death costs 50 XP up to level 5, 100 XP from 5 to 10. Calmed slimes reset to full HP (bfc013f). Open: can you lose a level, or does XP stop at 0 within the level? Penalty after level 10?
Status 1 Nov, 12:25 (UTC+2): respawn XP penalty in Core: 50 XP (levels 1-5), 100 XP (6+; above 10 is a placeholder), never below the level start or 0. Character.RespawnXpPenalty, LastRespawnXpLost.
Decided 1 Nov: the death penalty never removes a level; XP stops at 0 within the current level. Levels 1-5 lose 50, 6-10 lose 100; above 10 stays 100 (placeholder).
Status 1 Nov, 11:54: death XP penalty in 722f05d, shown on the fallen screen in 0fcd3fc (328 tests). Open gaps and balance points are logged as issues #30, #31, #33 to #37, #39 to #46, #48 and #51 for Dyllan to review.

### Added 1 Nov: Milestone "Adventurers start" (#52, #53)
- #52 Main Quest 1 "Adventurers start": after quest 4, the Colonel sends you through the woods to the Adventurers' Encampment to speak with Whiplash (0/1). Reward: 50 XP, 50 copper.
- #53 Adventurers' Encampment: second map. Green grass and tents, a cosmetic combat ring at the centre, three small tents around it, and a large tent to the north with Whiplash in front of it.
Open: Where does the forest connect to the camp (an exit on a map edge)? What does Whiplash say? Is the camp a rest spot for the bind point (#43)? Any enemies in the camp (assume none)?
- #52 Core done (1 Nov): quest 5 "Adventurers start" (id adventurers-start) offered by the Colonel after quest 4; generic Talk objective (QuestObjective.TalkTo, QuestLog.NotifyTalkedTo); Whiplash NPC id whiplash with placeholder dialogue (WhiplashQuest); completes on the talk, 50 XP + 50 copper. Map and screen are Mapmaker's (#53).
Decided 1 Nov: the route is Forest of Beginnings, then a path to the map edge, a scene transition, the Path map (filler, passive level 4 and 5 slimes the player can ignore), then from its edge to the Adventurers' Encampment. Whiplash uses filler text for now. Main Quest 1 rules in a4e50a0. Open: level 4 and 5 slime health and attack.
Decided 1 Nov: the forest exit is locked until Colonel Powers gives Main Quest 1 (#52). Placeholder slime stats: level 4 125 HP / 18 attack, level 5 150 HP / 21 attack.
- Path map slimes (1 Nov): Core slime levels 4 (125 HP, atk 18) and 5 (150 HP, atk 21) as placeholders; per-map range via Enemy.NewRandomSlime(rng, min, max), Path = Enemy.PathMinSlimeLevel..PathMaxSlimeLevel. Map and transitions are Mapmaker's.
- Leave-forest rule (1 Nov): Character.CanLeaveForest (true once Adventurers start is accepted or completed) and Character.LeaveForestRefusal placeholder; the exit check is Mapmaker's.

## First game milestone: "Up to the Pilgrimage" (set 1 Nov)
Goal: everything up to the pilgrimage the Colonel mentions in #52, about 3 to 4 hours of play, as the first full test build.
Done so far (about the first 20 to 30 minutes): creator, opening, Forest of Beginnings with tutorial quests 1 to 4, shop, titles, respawn, Main Quest 1, the Path and the Adventurers' Encampment (ca6eee7, 346 tests).
Needed before a 3 to 4 hour build is testable:
- Save and load (#42): a long run must survive closing the game.
  - #54 Save points (added 1 Nov): a "Load game" option on the main menu, and for now two save points, one near Colonel Powers (Forest of Beginnings) and one near Whiplash (Adventurers' Encampment).
- Slime XP and copper drops (#36) and the death penalty past level 10 (#30), so levelling and money keep going between quests.
- Defense (#34) and the unused stats (#40), so later fights scale.
- Inns or rest spots for bind points (#43), once there are towns.
- The story from Whiplash to the pilgrimage: quests, maps, enemies and NPCs, logged by Dyllan as issues.
- #56 Core (1 Nov): GameClock, Character.Clock, ticked from Combat.Update. HUD display is Mapmaker's.
- #57 Core (1 Nov): night = +10 monster levels (Enemy.LevelFor / NewSlime(base, clock)), slime stats to level 15 (placeholder), MapIds.IsSafeZone. Night is 7 pm to 8 am, day 8 am to 7 pm (confirmed by Dyllan).
- #55 Core (1 Nov): Barracks Master dialogue + BarracksMaster.Apply (Rest +3 h, Sleep until morning at night, Set as home). Mapmaker wires the NPC menu and the notices.
- #54 Core (1 Nov): SaveGame (JSON, version 1) capture/restore/Save/Load, SavePoints (save-colonel, save-whiplash). Mapmaker wires Load game, the save point interaction and a clock HUD.
- #54 to #57 Game wiring done (1 Nov, 532d4e5): clock HUD, night overlay and night slimes, Barracks Master menu, save point interaction and Load game on the main menu. Issues #54 to #57 are still open on GitHub for Dyllan to test and close.

- Decided (#30): death XP penalty is 200 from level 10 to 20, 400 from 20 to 30, doubling every ten levels.
- Decided (#31): angered slimes chase the player (give-up distance still open).
- #30 Core (1 Nov): respawn XP penalty 50 / 100 / 200 / 400 / 800, doubling every ten levels from level 11 (level 10 stays 100, boundary logged as a question). #31 Core (1 Nov): aggro'd slimes chase (Enemy.ChaseStepSeconds, UpdateEnemies walkability callback); leash/give-up is an open question. Both issues stay open for Dyllan.
- Decided: defense (#15, #34) is not planned for now; revisit with armour-based defense.
- Decided: enemies take standard damage (no 1.25× slime weakness); weaknesses may return later as a real mechanic.
- #32 (1 Nov): slimes take standard damage (multipliers 1.0), SlimeVulnerability retired; the Enemy multiplier mechanism stays for later weaknesses.
- #31 (1 Nov): the combat timer running out calms all aggro'd slimes (Dyllan); #33: Adventurer +2 stops one short of the next milestone; #36: slimes give 5 x level XP and drop one Slime Jelly (sell price, XP above level 3 and night XP open); #30 confirmed as built.
- Decided (#30): death XP penalty brackets 1-5: 50, 6-10: 100, 11-20: 200, 21-30: 400, doubling every ten levels (closed).
- Decided (#31): angry slimes chase; they calm down (full HP) when the 15 s combat timer runs out (closed).
- Decided (#33): titles add up to +2 per stat but never onto or past the next milestone (8→9, 9→9, 10→12) (closed).
- Decided (#36): slimes give 5 × level XP (levels 1-3) and one Slime Jelly (sell-only), no copper. Open: XP above level 3 and at night, jelly price and seller, full-inventory drops.
- #35 (1 Nov): stacking Death debuff in Core (-25/50/75% max health, energy and damage; 30 s then 60 s; level 3 no kill XP; saved); #36: Slime Jelly base price 2 copper, night XP moved to #60.
- Item stacking (1 Nov): all non-equipment items of the same id stack to 99 in one slot (confirmed by Dyllan), equipment never stacks; saves keep counts. Mapmaker shows the count on the slot.
- Decided (#35): Death debuff stacks to 3 (-25% 30 s, -50% 60 s, -75% 60 s and no kill XP); whole stack clears when the timer ends.
- Decided: Slime Jelly base price 2 copper; night XP moves to #60.
- Decided: all non-equipment items stack to 99 per slot; equipment never stacks.
- Idea (#61): carry weight; over the limit cuts movement 75% (intended punishment for hoarding, even though slimes then outrun you).
- #36/#62 (1 Nov): slime drops stay on the ground (GroundItems per map), manual pickup with "Inventory currently at capacity." when full, and Drop for inventory items. Mapmaker wires drawing, the pickup key and a Drop button.
- #36/#62 (1 Nov): Dyllan confirmed pickup by key, ground items vanish when the map refreshes (EnterMap on a different map clears them), only bag items can be dropped, a slider drops a chosen amount from a stack, quest items cannot be dropped.
