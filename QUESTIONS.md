# Open questions

Questions for Dyllan. Answer them in the matching GitHub issue or tell the team in the room, and we'll update the code and tick them off here.

## 1. Does race change stats? (#2)

Human, Elf and Dwarf are choosable but have **no stat effect yet**. There is a placeholder table, `StatDistributions.GetRaceModifier`, that returns all zeros. If races should change stats, what are the numbers for each?

## 2. Is there a cap on stats? (#2, #45) - ANSWERED
Dyllan (2 Nov, #45), verbatim: "No level cap and for now no restriction on names. No stat cap either." Nothing is capped: levels and stats grow without limit (tests push a level past 150 and a stat past 100). "For now" on names, so the rule may come back. Still open (not asked): can a stat go below a minimum?

## 3. Character name rules (#2, #45) - ANSWERED for now
Dyllan (2 Nov, #45), verbatim: "No level cap and for now no restriction on names. No stat cap either." So there is no length limit and no character restriction any more (`NameValidator`). Two things I kept, **placeholders**: the name still cannot be empty or blank (a nameless character cannot be saved), and control characters (tabs, new lines) are refused. `NameValidator.MaxLength` (20) is now only a suggested size for the text box (Mapmaker's `TextField` still stops typing at 20; raise it if long names are wanted).

## 4. Combat numbers (#11)

Placeholders until you confirm them:
- **Basic attack (Space)** hits one adjacent slime (8 tiles around you, or your own tile) with a 0.5 s cooldown.
- **Damage**: replaced by the #14 rules, see entry 8.
- **Slimes** now have level 1-3 stats (entry 10; they had 30 HP before #24). They do **not fight back yet**: should they? How much damage do they do, and should they respawn?
- **Slash** (hotbar slot 1 once learned): see entry 9 (#20): (10 + Physical Attack + n) x Strength modifier, 10 energy, 5 s cooldown, needs a weapon. Using Slash also starts the 0.5 s attack delay. It does nothing if no slime is next to you, and then costs nothing.
- **Energy** regenerates 2 per second (placeholder).

## 4b. Health and energy maximum (#12) - ANSWERED

Answered by Dyllan: **MaxEnergy**: first 5 x Wisdom, then changed with #17 to **50 x the Wisdom modifier** (same curve as health and damage), rounded half up. **EnergyBase = 50 is a placeholder** - is 50 the right base? **MaxHealth** was first 20 x Constitution, then changed with #15 to **100 x the Constitution modifier** (the same curve as damage: 0.1 per point, +1.0 per milestone at 10, 25, 50), rounded half up. Warrior (Con 13) = 230, Merchant (Con 9) = 90, Mystic (Con 5) = 50; Con 10 = 200, Con 25 = 450. Energy (#17): Warrior (Wis 7) = 35, Merchant (Wis 9) = 45, Mystic (Wis 13) = 115; Wis 10 = 100, Wis 25 = 225. Slash costs 10, so every class can afford it from full energy.

Note: reaching a milestone is a big jump (Con 9 to 10 is 90 to 200 health, +110; Wis 9 to 10 is 45 to 100 energy, +55). Current health rises by the same amount when a point is assigned, and drops by it when the point is given back.

## 5. Colonel Powers' other lines (#8)

Only the "I see you lack the skill..." line was given. We made up these, please correct them:
- while the quest is active: "Keep at it, {name}... Slimes slain {n}/5."
- after the quest is complete: "Well done, {name}. You have learned to survive... for now."

Also assumed: the Practice Sword is given (and auto-equipped to Main Hand) when you **accept**, only once; saying No keeps nothing, and talking to him again re-offers the quest without repeating the intro. Is the Practice Sword meant to be kept after the quest?

## 6. Inventory stacking and what "Slash" does (#8, #9)

Items that can be worn never stack. Items with a stack size above 1 would stack up to that size (none exist yet). The Slash ability is only recorded on the character for now: what does it do, and how is it triggered?

## 7. Levelling and free stat points (#13) - RESOLVED (stat points)

XP cost to go from level L to L+1 follows the issue: 50, 100, 150, 200 (L1-4), then 300 ... 700 (L5-9), then from L10 the step is 200 and doubles every 10 levels (L10-19 add 200: 900 ... 2700; L20-29 add 400: 3100 ...; L30-39 add 800, and so on). The numbers live in one class, `Leveling`.

Still placeholders, please confirm:
- **Stat points:** RESOLVED. Dyllan confirmed each level-up gives **3** points, but every 5th level gives **5 instead of 3**. The constants are `Leveling.StatPointsPerLevel` and `StatPointsOnBonusLevel`.
- **Points are spent one at a time into any of the eight stats**, with no cap. Assigned points can be taken back (Unassign) until the player leaves the menu or the game decides to lock them in; there is no "confirm" step in Core yet.
- **Health and energy:** when Constitution or Wisdom rises, the maximum follows the stat's modifier curve (so it jumps at milestones 10 / 25 / 50) and the **current** value rises by the same amount the maximum rose (so a full bar stays full). Giving a point back lowers the current value too, but never kills the player (health stays at least 1). Levelling up by itself does **not** heal.
- XP is kept as a running total (`Xp`); the bar uses `XpIntoLevel / XpForNextLevel`. Extra XP carries over into the next level.

## 8. Physical and magical damage (#14, #16)

Dyllan's rules, as built:
- **Stat modifier** = 0.1 per stat point **plus 1.0 for every milestone reached**. Milestones are 10, 25 and 50 (a list, `StatModifier.Milestones`). So 9 -> 0.9, 10 -> 2.0, 11 -> 2.1, 25 -> 4.5, 50 -> 8.0.
- **Base attack** is 10 for everyone. **Physical damage** = (10 + Physical Attack) x Strength modifier. **Unarmed** = 10 x Strength modifier. **Slash** = (10 + Physical Attack + n) x Strength modifier (entry 9). **Magic damage** = (10 + Magical Attack + n) x Intelligence modifier (entry 10).
- **Rounding:** the result is rounded **half up** to a whole number (10.5 becomes 11), once at the very end (so abilities are not rounded twice).

Placeholders, please confirm:
- The weapon bonus is **added to the base 10 before multiplying** (not multiplied after). The **Practice Sword's bonus is 5** (so 15 x modifier).
- Defence (#15) is not part of the damage yet.

Hits to kill a **30 HP** slime at the starting stats (Human, Strength 13/7/8) - OUTDATED since #24, see entry 10:

| Class | Unarmed | Practice Sword | Slash level 1 (sword) |
|---|---|---|---|
| Warrior (Str 13 = 2.3) | 23 dmg, 2 hits | 35 dmg, 1 hit | 46 dmg, 1 hit |
| Mystic (Str 7 = 0.7) | 7 dmg, 5 hits | 11 dmg, 3 hits | 14 dmg, 3 hits |
| Merchant (Str 8 = 0.8) | 8 dmg, 4 hits | 12 dmg, 3 hits | 16 dmg, 2 hits |

A Warrior with the sword kills every slime in one hit, which makes the tutorial quick; Mystics and Merchants need 3. Should slimes have more HP?

## 9. Abilities, levels and Tutorial Quest 2 (#19, #20)

Decided by Dyllan:
- **Slash** = round_half_up((BaseAttack + n) x Strength modifier), **BaseAttack = 10 + main-hand Physical Attack** (sword +5 = 15), **n = 5 + (level - 1)**. Level 1 Warrior with sword: (15 + 5) x 2.3 = 46. 10 energy, **5 s cooldown**. It replaces the old 1.5x rule.
- **Slash needs a weapon in the main hand.** Unarmed it is refused with the reason "Needs a weapon": no energy is spent, no cooldown starts, no use is counted.
- **Every cast that actually fires counts one use** (not no-target, cooldown, not-enough-energy or no-weapon casts).
- **Uses to level up:** 5 for level 1 -> 2, then 10, 20, 40 ... (doubling).
- **Energy Ball** (reward of quest 2): magic, (10 + Magical Attack + n) x Intelligence modifier, same n rule, **5 energy (was 15 until #23), 5 s cooldown**. No weapon needed (entry 10).
- **Tutorial:** after the 5th slime you must go back to Colonel Powers to turn the quest in; the 50 XP and Slash are given then (the sword stays). Quest 2 "Slashing Practice": "Use Slash 5 times on enemies.", progress "Slash used 0/5" (counts Slash casts that hit an enemy), reward Energy Ball + 50 XP, failure None.

My interpretations and placeholders, please confirm:
- **"5 uses, then 10, 20 ..." is counted within the current level**: UsesIntoLevel restarts at 0 on every level-up, so level 3 needs 15 uses in total (5 + 10), level 4 needs 35 (5 + 10 + 20). The total Uses is also kept. If you meant 10 total for level 3, it is a one-line change.
- Level 1 is the start; the table doubles forever (clamped around level 28 so numbers do not overflow). No maximum level.
- **Quest 2 is also turned in at Colonel Powers** (same as quest 1). His turn-in lines for quest 2 are placeholders ("Good, {name}. Practice is how you get strong... Take this, and use it well."), as are the lines while it is active ("Keep at it, {name}... Slash used 0/5.").
- If quest 2 is declined (No), the Colonel offers it again next time you talk to him, after repeating "You now have your first ability, why not test it out...".
- **Energy Ball is a placeholder**: its base power 5 and the 5 s cooldown (description and cost: see entry 10) follow the Slash pattern. Nothing else uses magic yet. The Warrior (Int 7) does (10 + 5) x 0.7 = 10.5 -> 11 per ball; the Mystic (Int 13) does 35.
- **Slimes respawn** at their original tile **10 s after dying** (placeholder delay; it waits while something is standing on the tile), so quest 2 can always be finished.
- Slash drops out of reach if the sword is unequipped mid-quest (the Colonel does not give a second one), so a player who sells or drops it cannot finish quest 2 until they get another weapon.

## 10. Weapons, Energy Ball, slimes and Quest 3 (#21, #22, #23, #24)

Decided by Dyllan:
- **Items** have separate **Physical Attack** and **Magical Attack** bonuses and a description. Practice Sword: Physical Attack +5. Practice Staff (weapon, main hand): Magical Attack +5. Both descriptions are Dyllan's text, verbatim (typos included).
- **Physical base attack = 10 + main-hand Physical Attack. Magic base = 10 + main-hand Magical Attack.** **Slash needs a weapon (any).**
- **Energy Ball:** (magic base + n) x Intelligence modifier, n = 5 + (level - 1), **5 s cooldown, 5 energy** (was 15), description "Conjure an orb of pure energy."
- **Quest 3 "Energy Ball practice"** (turn in at the Colonel): perform Energy Ball 5 times, progress "Energy ball 0/5", reward 50 XP + 50 copper, failure none, Accept Yes/No. The Colonel's speech after turning in quest 2 is Dyllan's text, and the player gets a Practice Staff.
- **Slimes** are level 1-3 at random per spawn: L1 50 HP / 10 attack, L2 75 / 12, L3 100 / 15 (HP first said 100/125/150, changed by Dyllan on 1 Nov). Enemies have separate Physical and Magical multipliers; slimes now take standard damage (see the vulnerability entry below).

My interpretations and placeholders, please confirm:
- **Slime vulnerability: RETIRED (#32, confirmed by Dyllan 1 Nov): enemies take standard damage for now, no weakness modifier.** Slimes' physical and magical multipliers are 1.0 (the old 1.25x placeholder and `Enemy.SlimeVulnerability` are gone). The multiplier mechanism stays in `Enemy` (`PhysicalMultiplier`, `MagicalMultiplier`, `ResistanceLabel`) for weaknesses and resistances that may return later; the damage tests still cover it with a custom enemy.
- **The multiplier is applied before rounding**, so rounding happens once (with a 1.25 multiplier a Warrior's sword basic attack is 15 x 2.3 x 1.25 = 43.125 -> 43 and Slash 57.5 -> 58; slimes now use 1.0, so 35 and 46).
- **Slimes store their attack (10/12/15) but still do not fight back.** Is that right for now? Should slimes attack, and does the number mean damage per hit?
- **Slime level is uniform random 1-3** (a seeded `Random` can be passed in). **A respawned slime rolls a new level** (placeholder), after the usual 10 s.
- **Energy Ball needs a staff in the main hand (#39, Dyllan 2 Nov, answered)**: the issue "Gap: Does Energy Ball need the staff?" got "Yes it does...". Built: without a staff the cast is refused with "Needs a staff" and costs nothing. **PLACEHOLDER:** a "staff" is a main-hand weapon with Magical Attack above 0 (there is no staff item type); a sword is not a staff. The staff adds its Magical Attack to the base as before. (The older placeholder "does not need a staff" is retired.)
- **When the staff is given, a popup (#39):** Dyllan, verbatim: "When staff is given have a message pop up that reads, Select I to open up character sheet, equip the staff..." Corrected: "Press I to open your character sheet and equip the staff." (`ColonelPowersQuest.StaffNotice`, returned by `Accept`; Mapmaker shows it as a popup.) The "..." at the end of his sentence is dropped as a guess.
- **Still open (#39): does the staff count as a weapon for Slash?** Today any main-hand weapon counts, so Slash works with the staff equipped (placeholder).
- **Quest 3 counts every Energy Ball cast that fires on an enemy** (a target next to you is required, like quest 2), whether or not it kills. Slash casts do not count.
- **The Practice Staff is given when you accept quest 3** (not when you turn in quest 2), matching how the sword is given at quest 1. It goes into the inventory and is **not equipped** (the sword stays equipped). **If the inventory is full**, accepting is refused with the notice "Your inventory is full", the quest stays Offered, and talking to the Colonel again re-offers it. The staff is only ever given once.
- **The speech is split into 3 dialogue boxes** at Dyllan's "..." pauses (the text joined is unchanged). Because quest 3 is offered straight after the speech, the same speech is repeated if you say No and talk to him again.
- **Placeholder Colonel lines for quest 3** (Dyllan gave none): while active "Keep at it, {name}... Energy ball 2/5."; turn-in: now Dyllan's quest 4 speech (#25, see entry 11). The quest 2 turn-in filler lines ("Good, {name}. Practice is how you get strong...") are no longer used: they were replaced by the speech.
- **Slash with a staff** works (any weapon counts) and is (10 + 0 + n) x Strength modifier: the staff adds nothing physical. A basic attack with a staff equals an unarmed attack.
- **Copper** is the currency (starts at 0, never negative). Nothing spends it yet; it has no UI. Is "copper" the lowest coin, and are there silver and gold?

Hits to kill (level 1 abilities, starting stats, standard damage (multiplier 1.0), slime HP 50 / 75 / 100; sword Slash = (10+5+5) x Str, staff Energy Ball = (10+5+5) x Int):

| Class | Slash with sword | vs L1 / L2 / L3 | Energy Ball with staff | vs L1 / L2 / L3 |
|---|---|---|---|---|
| Warrior (Str 13, Int 7) | 46 | 2 / 2 / 3 | 14 | 4 / 6 / 8 |
| Mystic (Str 7, Int 13) | 14 | 4 / 6 / 8 | 46 | 2 / 2 / 3 |
| Merchant (Str 8, Int 9) | 16 | 4 / 5 / 7 | 18 | 3 / 5 / 6 |

A Warrior with the sword needs 2 basic attacks (35 each) for a level 1 slime; unarmed 23 each needs 3.

## 11. Quest 4, consumables, titles and shops (#25, #26, #27, #28)

Decided by Dyllan (his text is verbatim, typos included):
- **Quest 4 "Shopping before adventure"** (turn in at the Colonel): description "Colonel Powers has a potion of lesser healing he is willing to sell to you.", progress "Potion of lesser healing baught 0/1", reward **Title Adventurer + 50 XP + Adventurer's rations**, failure none. Offered after the Colonel's two lines on turning in quest 3 ("you might just be a believable spell slinger." and "Now one cannot survive with out economic power, ...").
- **Potion of lesser healing**: "A dull red colour. Tastes oddly like strawberries.", heals 50 HP (capped at max health), **shared 5 s use cooldown**, stackable, usable from the inventory and the hotbar.
- **Title Adventurer**: "You have completed the tutorial. The Adventurer is one who is prepared for the upcoming adventure.", **+2 to all stats while equipped**. One title worn at a time; it can be taken off.
- **Shop**: after accepting quest 4 the Colonel offers **Talk / Shop**. Talk says "Stop waiting time and let's get moveing.". Shop has **Buy and Sell tabs (Sell disabled)**, stock is the Potion at **25 copper**, and selecting an item offers **Buy / Haggle (Haggle disabled)**. Buying refuses with "Not enough copper" or "Inventory full" and changes nothing.

My interpretations and placeholders, please confirm:
- **Potion stack size is 99** (the general stack cap, confirmed; was a placeholder 10).
- **Using a potion at full health is refused** ("Already at full health") and nothing is spent, so the cooldown does not start either. Other refusals: "On cooldown", "None left", "Cannot be used". Should it be allowed at full health instead?
- **The 5 s cooldown is shared by all consumables** and is independent of the ability cooldowns. It ticks while the game updates. Every hotbar slot holding an item shows it.
- **Hotbar:** a slot holds an ability OR an item. Nothing puts a potion on the bar by itself; the inventory/hotbar screen must call `Combat.AssignItemToHotbar`. Keys 1-5 then use it with no enemy target needed. The slot keeps the item even when the stack runs out ("None left").
- **Adventurer's rations (#26):** "When outside of combat, recover completely. Up till level 10. After that the rations are only half as effective." A usable consumable (stack 99, shares the 5 s item cooldown). Level 10 or lower: restores health AND energy to max. Above level 10: restores 50% of the max health and 50% of the max energy (capped at max, rounded half up for health, down for whole energy). One is used each time.
- **In combat** = dealt or took damage within the last **15 s** (Dyllan, 1 Nov; `CombatController.CombatDurationSeconds`; the window restarts on every hit and counts down while the game updates). Rations are refused with "Can't use in combat" while it runs. Rations restore both health and energy.
- **Rations refusals** in this order: "Can't use in combat", "On cooldown", "Already fully recovered" (health AND energy both full; if only one is missing they work). Nothing is spent when refused.
- **Rations text:** Dyllan's sentence is the item description (there is no flavour line); the effect lines show only "Cooldown after use 5s" so the sentence is not printed twice. Name written with the apostrophe ("Adventurer's rations"), the issue says "Adventurers rations": which spelling do you want?
- **The reward text** reads "Title Adventurer, 50 XP, Adventurer's rations" (the issue says "50 Exp"; the game says "XP" everywhere). The title is **earned, not worn automatically**: the player equips it (the titles screen).
- **If the rations do not fit** in the inventory, the quest stays ready to turn in, nothing is given, and the notice is "Your inventory is full" (like accepting a quest). Make room and talk to the Colonel again.
- **Quest 4 counts a Potion of lesser healing bought from any shop** (not only the Colonel's). Only one purchase is needed.
- **Colonel placeholder lines** (Dyllan gave none): above Talk / Shop "What do you need, {name}?"; quest 4 turn-in "Good, {name}. You have shopped like a true adventurer... Take this for your trouble.". The speech is split into two boxes after "spell slinger." and is repeated if you say No to quest 4.
- **The Talk / Shop choice stays after quest 4 is done** (the old "done" line is no longer used). While quest 4 is ready to turn in, talking hands it in first, then the next talk shows the choice.
- **The shop's stock is unlimited**; it sells one at a time. The shop name "Colonel Powers" is a placeholder. Core has no screen: the Shop option ends the dialogue with the action `shop:colonel-powers-shop`, and the game has to open the shop screen (not built yet, so Shop currently does nothing in the game).
- **Titles and stats:** `Stats` stays base + assigned points (what the I menu shows). `EffectiveStats` adds the title and is what modifiers, max HP/EN and damage use. **Milestones (10 / 25 / 50) count the effective stats**; since #33 the title can no longer carry a stat onto or past the next milestone (see the #33 section below). Equipping or taking off a title moves current HP/EN with the maximums like assigning or taking back a point does (taking it off never drops health below 1).
- **Slimes are passive and then fight back (Dyllan, 1 Nov).** A slime never attacks until the player damages it; from then on (until it dies) it is aggro'd and hits the player for its level attack (10 / 12 / 15) **every 6 s** (was 3 s), **the 6 s is a cooldown (Dyllan, 1 Nov): the first hit is immediate the first time the player is in reach (own or adjacent tile), then it waits 6 s** (earlier text said the first hit came 6 s after aggro). A respawned slime is a new slime, so it is passive again. The damage comes straight off Health (no defense yet, #15) and counts as damage received, so it starts the 15 s combat window.
- **Placeholders in the slime fight:** a slime only hits when it is on a tile next to the player (or on the player's tile), the same reach as the player's attack, and reach is checked before every attack; out of reach it does not attack and stays ready (cooldown 0), so it hits as soon as you are back in reach. Aggro does not time out. Whether slimes chase the player is up to the screen (not decided).
- **Death and respawn (#29, Dyllan's text):** at 0 health the message "You have fallen... The goddess has seen fit to grant you another chance." shows, and the character **respawns automatically 5 s later** at the bind point (`Character.FallenMessage`, `RespawnDelaySeconds`, `Respawned`). One bind point at a time (`SetBindPoint` replaces it); a new town does not change it. Default: the free tile south of Colonel Powers in the forest.
- **Respawn XP penalty (Dyllan, 1 Nov):** each respawn costs **50 XP at levels 1-5 and 100 XP at levels 6-10** (`Character.RespawnXpPenalty`; the XP actually taken is `LastRespawnXpLost`). Respawn is at full health and energy. **Confirmed by Dyllan (1 Nov):** no level loss; **XP stops at the start of the current level** (and never goes below 0), so you may lose less than 50/100, even 0. **Above level 10 (#30, Dyllan 1 Nov: "200 exp from 10 to 20, then 400 20 to 30. Every ten levels double the cost"):** levels 11-20 lose **200**, 21-30 **400**, 31-40 **800**, doubling every ten levels (`Character.RespawnXpPenaltyForLevel`). **Confirmed by Dyllan (1 Nov, "shift the levels up then 1 to 5 then 6 to 10 etc."): the brackets are 1-5 = 50, 6-10 = 100, 11-20 = 200, 21-30 = 400, 31-40 = 800**, so level 10 stays 100 and the 200 starts at level 11. Nothing open.
- **Other respawn placeholders, please confirm:** no copper or item loss and quest progress stays; the **combat window is cleared**; **every slime is made passive again and healed to full HP** so dying repeatedly cannot wear one down (the screen calls `CombatController.CalmEnemies` from the `Respawned` handler; dead slimes stay dead until their normal respawn); the item and attack cooldowns keep running. While fallen you cannot use items ("Can't use while fallen").
- **Bind points:** only the default (next to the Colonel) exists; inns and rest locations do not exist yet, so nothing calls `SetBindPoint` in the game. Bind points are not saved anywhere yet (there is no save system). Should dying far from the bind point (a later map) show the map change?

### Quest 5 "Adventurers start", Core side (#52)
Dyllan's text is verbatim, typos included. Typos kept as written: **"Your barely passable"** (for "You're"), **"moveing"**, **"Adventurers encampment" / "Adventurers camp"** (no apostrophe, so also not "Adventurers'" in the quest), **"Exp"** (shown as XP like everywhere else: "+50 XP, +50 copper"), and **"Failure condition none"** (lower case; stored as "None" like the other quests).
- **Offer:** the Colonel offers it when quest 4 is handed in. His line is the end of the quest 4 turn-in speech (after the placeholder "Good, {name}. You have shopped like a true adventurer... Take this for your trouble."), split into two boxes after "pilgrimage." and repeated if you say No.
- **Turn-in decision (placeholder, please confirm):** the issue only says to speak with Whiplash, so **talking to Whiplash completes the quest and pays 50 XP + 50 copper on the spot**; nobody has to be returned to. If you want the player to go back to the Colonel to collect, it is a one-flag change (`completesOnObjectives` in `QuestLibrary.AdventurersStart`).
- **While it is active** the Colonel shows the Talk / Shop choice (no progress line). Talking to Whiplash **before accepting** counts nothing; it counts once the quest is Active.
- **Whiplash's dialogue is a placeholder** (Dyllan gave none): "So the Colonel sent you. I am Whiplash." / "Keep your head down and your blade sharp, {name}, and I will advise you as we move forward." while the quest is active, and "Stay sharp out there, {name}." at any other time. What does Whiplash really say, and does he give the next quest?
- The NPC id is `whiplash` and the map id `adventurers-encampment` (Mapmaker, #53). Core does not check which map the talk happens on.

### Path map slimes, levels 4 and 5 (Dyllan, 1 Nov)
A filler "Path map" between the forest and the encampment is filled with passive level 4 and 5 slimes (Mapmaker builds the map and transitions).
- **PLACEHOLDER stats** (Dyllan gave level 4 and 5 only as placeholders): **level 4: 125 HP, attack 18; level 5: 150 HP, attack 21**. Levels 1-3 are unchanged (50/75/100 HP, attack 10/12/15).
- Same as the forest slimes: **standard damage (1.0x, vulnerability retired)**, **passive until hit**, then hits every **6 s**, same aggro, calm and respawn rules.
- **The level range is per map**: `Enemy.NewRandomSlime(rng)` stays forest 1-3; `Enemy.NewRandomSlime(rng, minLevel, maxLevel)` takes a range (Path: `Enemy.PathMinSlimeLevel`..`PathMaxSlimeLevel` = 4..5). Uniform over the range, as in the forest.
- The level tag colour (`Hud.SlimeLevelColor`) has only green/yellow/orange, so level 4 and 5 show orange like level 3 until Mapmaker picks colours.
- **Leaving the starting forest (Dyllan, 1 Nov):** the player cannot leave the Forest of Beginnings until Colonel Powers has given "Adventurers start" (**accepted or completed counts**; offered but not yet accepted does not). `Character.CanLeaveForest`. **PLACEHOLDER refusal text** (Dyllan gave none): "Colonel Powers isn't finished with you yet." (`Character.LeaveForestRefusal`). The forest exit (Mapmaker) must check it and show the text; Core does not block movement itself.

### Game clock (#56)
Dyllan: "Starts at 8 am and runs 5 times faster then normal time."
- **Day 1 starts at 8:00 am. 1 real second = 5 game seconds** (so a game hour is 12 real minutes and a day is 4 h 48 min of real time). `Character.Clock` (`GameClock`): `Day`, `Hour` (0-23), `Minute`, `TimeText` ("8:00 am"), `Text` ("Day 1, 8:00 am"), `Advance(hours)`, `AdvanceToMorning()`, `Update(realSeconds)`.
- **It is ticked from `Combat.Update`**, which the Game already calls every frame, so it **pauses whenever the Game stops calling update** (menus, dialogue, shop). **Placeholder question: should the clock also stop during dialogue/shop?** (that depends on whether the screen keeps calling `Combat.Update` there.) It keeps running while the character is fallen.
- **The time format ("8:00 am", "Day 1, 8:00 am") is a placeholder.**

### Day and night (#57)
Dyllan: "Day: During daylight hours, 8 am to 6pm monsters are at there original levels. Night time: 7pm to 7 am, monsters are ten levels stronger. Monsters cannot enter safe zones. Will be established later 'Non combat zones' and Wild zones. Can rest at inns in the former, can camp out fir the later. Details to be specified later" (typos "there", "fir" are Dyllan's).
- **Confirmed by Dyllan (1 Nov):** **night is 7 pm to 8 am, day is 8 am to 7 pm, no gaps** (the issue text's "6pm" and "7 am" are superseded). `GameClock.IsNight` is true from 19:00 to 07:59; `NightStartHour` = 19, `NightEndHour` = 8.
- **Night = base level + 10** (`Enemy.NightLevelBonus`; `Enemy.LevelFor(baseLevel, clock)`, `Enemy.NewSlime(baseLevel, clock)`, `Enemy.NewRandomSlime(rng, min, max, clock)`; `Enemy.BaseLevel` and `IsNightEmpowered` tell the screen it is a night slime). Forest slimes become 11-13 and Path slimes 14-15 at night.
- **Slime stats above level 5 are placeholders** that continue the pattern **+25 HP and +3 attack per level**, up to level 15: level 6 = 175 HP / 24, level 11 = 300 / 39, level 15 = 400 HP / attack 51. (Level 4 = 125 / 18 and level 5 = 150 / 21 were already placeholders; levels 1-3 are Dyllan's.) Standard damage (1.0x) and a 6 s interval. Kill XP is 5 x level (#36 section below).
- **Existing live slimes keep their level** when night falls or ends (placeholder); **new spawns and respawns use the time of the spawn**.
- **Safe zones:** `MapIds.IsSafeZone(mapId)` / `MapIds.AllowsMonsters(mapId)`. **Only the Adventurers' Encampment is a safe zone (placeholder);** the forest and the path are wild zones. Nothing chases yet, so for now it is a flag (the encampment simply has no slime spawns). Inns/camping rules are "to be specified later" and are not implemented.

### Barracks Master and new bind point (#55)
Dyllan: "Have another area in the encampment that looks like a bunch of beds or mattresses. Have an NPC stand before it called the barracks master. When speaking to him. ... Then a menu options appear. Rest / Set as home. Rest: recovers fully and progresses the game clock by 3 hours. At night option added  to transition to the new day. Awakening at 8 am. Set as home: sets as new bind point." Typos/oddities kept: **"Have another area"** (read as "Have another area"), **"When speaking to him."** (sentence ends there), **"Then a menu options appear"**, **"Set as home"** (no full stop in the menu), **"added  to"** (double space). The issue title is "New bind point."
- NPC id `barracks-master` (speaker "Barracks Master"), map `adventurers-encampment`. His welcome line is verbatim: "Welcome adventurer to the Adventurers encampment. Until you are prepared for the pilgrimage you may rest up here and train." The options sit under that line.
- **Rest:** health and energy to full, clock +3 hours (may run past midnight into the next day). **Sleep until morning (PLACEHOLDER label)** appears only at night (7 pm to 7:59 am, see #57): same recovery, then the clock jumps to **8:00 am**: the next day if it is before midnight, the same day if it is already after midnight. At 7:59 am it wakes you at 8:00 am the same day; exactly 8:00 am is day, so the option is not offered.
- **PLACEHOLDERS:** a **Leave** option is added (Dyllan listed only Rest and Set as home, but the player needs a way out); the notices ("You rest. Health and energy restored", "You sleep until morning: Day 2, 8:00 am", "Home set: this is where you wake after a fall"); **a fallen character cannot rest or sleep** ("You cannot rest while fallen"). Rest is allowed at any time of day and in combat (the encampment has no monsters).
- **Set as home** uses the bind point of a free walkable tile next to the Barracks Master (`BarracksMaster.HomePointFor(map, masterX, masterY)`, east of him today because the beds are south). Dyllan said "by the beds"; say if it should be among the beds instead.
- **Nothing else changes while sleeping:** no hunger, no quests, no slimes respawn extra.

### Save points and Load game (#54)
Dyllan: "Add a menu option to main menu called load game. For now place two save points, one near Colonel Powers, the other Near Whiplash" (title "Save points"; "Near" is capitalised in the original).
- **Save points:** ids `save-colonel` (Forest of Beginnings, tile (24,12) next to Colonel Powers) and `save-whiplash` (Adventurers' Encampment, tile (22,7) next to Whiplash). The tiles are Mapmaker's 'S' tiles (`TileMap.SavePoints`); `SavePoints.FindNear(mapId, map, playerX, playerY)` says which one the player can use (standing on it or next to it, diagonals too). Interacting = pressing the talk key near it (placeholder, Dyllan gave no key).
- **PLACEHOLDERS (the single `save.json` is replaced by slots, see Save slots below):** one save slot, `save.json` in `WarInLocked` under the user's application data folder (`SaveGame.DefaultPath`; Windows %APPDATA%, Linux ~/.config); saving again overwrites it without asking; **you cannot save in combat or while fallen** ("You cannot save in combat" / "You cannot save while fallen"); the notice "Game saved"; load errors "No save found." / "This save is damaged." / "This save is from a newer version of the game."
- **What is saved:** name, race, distribution, stats and assigned points, free points, level, XP, health, energy, copper, inventory (slot, item, count), equipment, hotbar (abilities and items, not cooldowns), abilities (level and uses), quests (state and objective progress), titles and the equipped one, flags, bind point, the game clock, and the current map and tile. **Not saved:** cooldowns, the combat window, live slimes (they respawn at their spawn tiles, at the then-current level), the fallen countdown. A save made while fallen cannot happen (refused), but a hand-edited one loads with 1 health.
- **Loading** restores the position exactly (map and tile). Items, quests or titles that the game no longer knows are skipped. The file has a `Version` field (now 1); newer versions are refused. **Questions:** several save slots? an "Are you sure?" before overwriting? should saving heal or advance the clock? should Load game also be offered after "New game" (i.e. Continue)?
- **Quests are rebuilt from `QuestLibrary.Create(id)`**, so a new quest needs a line there as well as in `ToCharacter` (done once, centrally).

## Collision between the player and enemies
Dyllan (1 Nov): the player cannot move onto a tile held by a living enemy, and enemies cannot move onto the player's tile. Core helper: `WarInLocked.Core.Combat.TileOccupancy` (`IsBlocked`, `IsBlockedForEnemy`, `TryMoveEnemy`). Guesses to confirm:
- **Enemies also block each other** (one body per tile), not only the player. Say if two slimes may share a tile.
- **Dead enemies never block** and a dead enemy cannot be moved.
- **Walking onto a tile with a living enemy is simply refused** (no bump attack, no push). The player can still hit it from the neighbouring tile.
- If the player respawns or loads onto a tile that already holds an enemy, Core does not move anyone; the screen should place the player on a free tile (the attack reach still counts the own tile).
- Slimes stand still for now; chasing is #31.

## Quest markers (Dyllan, 1 Nov)
Yellow **!** = a quest can be taken from the NPC; grey/black **?** = the player has an unfinished quest from, or to hand in to, the NPC; yellow **?** = a quest can be handed in to the NPC. Core: `QuestMarker { None, Available, InProgress, ReadyToTurnIn }` and `QuestMarkers.MarkerFor(npcId, character)` (also `character.Quests.MarkerFor(npcId)`). Mapmaker draws the symbols. Guesses to confirm:
- **Priority when several apply: ReadyToTurnIn > Available > InProgress** (Dyllan did not say).
- **Available** also covers a quest that was shown but declined (Offered), and the very first meeting with the Colonel (nothing in the log yet). The next quest only counts once the one before it is Complete (the tutorial quests are a chain).
- **"Adventurers start" shows yellow ? on Whiplash while it is Active** (talking to him completes it). **The Colonel shows a grey ? meanwhile** (he gave it and it is unfinished); it vanishes when Whiplash completes it.
- Whiplash has no marker at any other time. NPCs without quests (Barracks Master) never show one. Quest giver / turn-in NPC ids live in `QuestLibrary` (`GiverNpcId`, `TurnInNpcId`, `PrerequisiteId`); a new quest giver means adding to them.
- A ready quest keeps its yellow ? even if the reward items do not fit in the inventory (turning in then just reports "Your inventory is full").

## Angered slimes chase the player (#31)
Dyllan (1 Nov): "Have slimes that have been angered chase the player." Core: `CombatController.UpdateEnemies(enemies, seconds, playerX, playerY, isWalkable)`. An aggro'd, living slime that is out of reach steps one tile toward the player every `Enemy.ChaseStepSeconds` (0.5 s, **placeholder**), 8 directions, never onto the player or another living slime (`TileOccupancy`). Once next to the player it stops and fights (first hit immediate, then the 6 s cooldown). **Answered by Dyllan (1 Nov): "If you run out the combat timer, then the slimes lose there aggro."** When the 15 s out-of-combat timer expires (no damage dealt or taken), every aggro'd slime calms (`Enemy.Calm()`: passive again, healed to full, stops chasing); it stays where it is and wanders again. There is no distance leash. Still placeholders:
- **Pace:** 1 tile per 0.5 s (2 tiles/s) is a guess; the player's walking speed is the screen's.
- **Pathfinding:** none. A slime steps straight at the player (diagonals allowed, sidestepping when blocked) and can get stuck behind a wall; no route-finding yet.
- **Diagonal steps never cut a wall corner** (both tiles beside the step must be walkable). A guess.
- Slimes never chase into safe zones or other maps: slimes live in one map's list and do not follow through doors.

## Adventurer title and milestones (#33)
Dyllan (1 Nov): the title's +2 stops short of the milestone. Per stat: bonus = max(0, min(2, nextMilestone - 1 - base stat)), milestones 10 / 25 / 50, where base = creation + assigned points (`StatModifier.LimitedBonus`, `Character.TitleBonus(stat)`, `Character.TitleBonusFor(title, stat)`). Examples: 10 -> 12, 8 -> 9 (+1), 9 -> 9 (+0), 23 -> 24, 24 -> 24, 25 -> 27; invest two points into 8 to reach 10 and the full +2 applies again (12). The rule is in the title's description text (`TitleLibrary.AdventurerBonusExplanation`). Above 50 there is no milestone, so the full +2 applies. I read "invest to 9 stays 9" as the bonus dropping from +1 to +0 (effective 9 either way). Open: for a stat sitting at 9 there is no bonus at all, as Dyllan described it.

## Slime kills: XP and Slime Jelly (#36)
Dyllan (1 Nov): level 1 slimes give 5 XP, level 2 give 10, level 3 give 15, and each kill drops one Slime Jelly regardless of level, which can only be sold; no currency drops from monsters. Core: `CombatResult.XpGained`, `LevelsGained`, `DroppedItem`, `DropLost` on a kill; XP is added to the character at once and the jelly goes into the inventory (it stacks). Questions and placeholders:
- **XP for levels 4 and 5 confirmed (Dyllan: "Level 4 and 5 Slimes follow the same pattern for now. Increasing by 5 over the last level.")**: 20 and 25. Night slimes: Dyllan, "We will discuss night time in another task." (#60). Until then night slimes give 5 x their raised level (a night level 1 slime gives 55), a placeholder.
- **Slime Jelly base sell price is 2 copper** (`ItemLibrary.SlimeJellySellPrice`; Dyllan: "For now slime jellies sell usually for 2 coppers. No shops exist to sell to yet though and the price can vary depending on the shop."). It stacks to 99 (the general stack cap) and is a Misc item with the text "A wobbling blob of jelly. Good for nothing but selling." (my placeholder text). The shop's Sell tab is still disabled (#28) and no shop buys it yet; when shops get a sell side they may pay more or less than the base 2.
- **Full inventory: answered (see "Ground items" below).** The jelly now stays on the ground where the slime fell; the old "drop is lost" rule is gone. The XP is still given and the kill still counts for quests.
- Kills now give XP, so quest 2's turn-in XP (50) comes on top of the kill XP (5 per level 1 slime): the tutorial now reaches level 2 after the turn-in with 75 XP.

## Death debuff (#35)
Dyllan (1 Nov), verbatim: "Add death debuff, cutting health, attack power and energy by 25%, lasts 30s, and it stacks. If a player dies well it is in effect they earn Death debuff 2 which has the decrease increase to 50% with a minute cooldown. Level 3 is max, minute cool down remains, decrease is 75 but at level 3 no experience can be earned well debuff is active not including quest exoerience. Once timer expires, entire stack is removed." (Typos kept: "well" for "while", "exoerience".)

Built (`DeathDebuff`, `Character.DeathDebuffLevel / DeathDebuffRemaining / DeathDebuffMultiplier / DeathDebuffName / DeathDebuffText / EarnsKillXp / DeathDebuffChanged`): each respawn adds a level. Level 1 = -25% for 30 s, level 2 = -50% for 60 s, level 3 = -75% for 60 s with no XP from kills (quest XP still counts); when the timer ends the whole stack goes. Text example: "Death debuff 2: -50% health, attack and energy (0:42)" (level 3 adds ", no XP from kills"). The advisor's reading on the issue asked three questions; my answers are guesses:
- **Dying at level 3 stays level 3 and restarts the 60 s timer.** (guess)
- **The cut applies to max health, max energy and damage dealt (basic attacks and abilities, rounded once at the end). The respawn fills to the reduced maximum** (so 75% of 230 = 173 health at level 1). When the debuff ends the maximum comes back but current health and energy are **not** refilled (they regenerate or heal as normal). (guess)
- **It stacks with the XP death penalty: both apply** on every respawn. (guess, and so the answer to "does it stop XP loss at level 3": no)
- **The timer runs in real game time**, in or out of combat, driven by `Combat.Update`. **Menus must not pause it (confirmed, see below).** Whether dialogue pauses it is open. It is **frozen while fallen** (the 5 s wait), and the level is added at the moment of respawn. (guess)
- **Saved:** level and remaining seconds are in the save file (`DeathDebuffLevel`, `DeathDebuffRemaining`; older saves load with none).
- **Confirmed by Dyllan (2 Nov):** (1) "Fir now, level 3 death just resets current debuff timer." (corrected: "For now, a level 3 death just resets the current debuff timer"); (4) "To note, there shouldn't be a pause in game play when menues open. It should continue to run. No pausing." (corrected: "menus"): the debuff timer, the game clock and combat keep running while any menu is open (**Game side, done by Mapmaker in c199e26**: the world keeps ticking under the I, K, Titles and Shop menus); (5) "Resting at a barracks does not terminate the debuff," (the trailing comma is his). The other guesses stand ("Most of the guesses can stay.").
- **Still open: does dialogue also not pause?** He only said menus. Placeholder: the Colonel's dialogue box still freezes the world (slimes wait, the timer is not ticked) as the Game does today.
- Not specified, so guessed (resting at the barracks is now confirmed): **resting at the barracks and Adventurer's rations do not remove the debuff**; the max-energy cut also lowers the energy ceiling for regeneration; the cut is on the character's maximums, so ration/heal amounts are capped at the reduced maximum.
- Unanswered: should the debuff be removable (rest, item, shrine), and should it show on the respawn screen? (Core exposes the text; the screen decides.)

## Item stacking (1 Nov)

Dyllan, verbatim: "Oh item stacking, add it in, items of the same thing stack, except equipment. They do not stack in inventory." He then set the cap: **99 for all stackable items** (confirmed, not a placeholder).

- Every item that is not equippable stacks (same id, one inventory slot) up to **99** (`Item.DefaultStackSize`, `Item.MaxStack`): potions, rations, Slime Jelly, quest or misc items. A 100th goes into the next free slot.
- Equipment (anything equippable) never stacks: one per slot, and an equippable item cannot be given a stack size. Unequipping puts one item back in a slot.
- Adding tops up existing partial stacks first, then uses free slots; using an item takes one from the stack (the slot empties at zero); `RemoveItem` takes from the last stacks first. Save/load keeps every slot's count; a saved stack above today's cap spills into other slots instead of vanishing.
- Guess: all non-equipment items share the one cap (no per-item caps). A full inventory still accepts an item that stacks onto a partial stack.

## Ground items, picking up and dropping (#36, #62)

Dyllan (1 Nov), verbatim: "enemies slain leave the items where they are dropped, player choses to pick them up or not. If players inventory slots are all filled a message pops up. 'Inventory currently at capacity.' With this also add a drop functionality to inventory items, players can drop items to pick up new ones."

Built (Core): kills put the drop on the ground at the slime's tile (`GroundItems`, one per map); `Character.PickUp(x, y)` / `GroundItems.TryPickUp`; `Character.DropItem(slot, count, x, y)`. With no room the item stays and the message is exactly `Inventory currently at capacity.` (`GroundItems.CapacityText`). This answers the #36 full-inventory question. Guesses:

- **Confirmed by Dyllan, verbatim:** "Pickup by key press, not automatically when walked on." Mapmaker picks the key (suggest E). Core picks the nearest item on the player's tile or the 8 neighbouring tiles (own tile first, then straight, then diagonal), my guess for the reach. Walking over items never collects them.
- **Partial fit:** a stack with some room takes what fits and leaves the rest on the ground; the capacity message is shown too (the bag is now full).
- **Confirmed by Dyllan, verbatim:** "Ok items vanish when map refreshes. Leaving then returning. They remain so long as you do." Core: `Character.EnterMap` wipes all ground items when the map id changes (including coming back to a map you left); calling it for the map you are already on keeps them. Ground items are not saved (loading a save starts with empty ground) and have no despawn timer. **My decision: respawning at a bind point on the same map keeps the items** (the Game only reloads a map on respawn when the bind point is on another map, and that wipes them).
- **Fallen (dead) player can neither pick up nor drop.**
- **Drop:** takes a count from a slot (more than owned drops the whole stack) and puts it on the player's own tile; same items on one tile merge up to 99. **Confirmed by Dyllan, verbatim:** "Only items in your inventory slots can be dropped, anything currently equipped must be unequipped before being dropped." (Worn equipment is not in the bag, so Core already refuses it.) **Confirmed by Dyllan, verbatim:** "If you have more then one item in a stack, open a slider to drop a specific amount. Quest items can't be dropped." (Typo "then" kept.) Core: the `count` argument of `DropItem` is the amount; the slider is Mapmaker's job (show it only when the stack count is above 1, range 1 to the count; `DropStack` drops it all). Quest items cannot be dropped (`Item.IsQuestItem`; none exist yet; the text "Quest items can't be dropped" is mine). No confirmation step in Core.
- Dropped items can be picked up again by anyone nearby (just the one player exists).

## Save slots: autosave and three manual saves (#42)
Dyllan (2 Nov), verbatim: "If we can implement an auto save. Lets do that, for manual save slots let's say 3, the top one will be the auto save slot and there will only be one. So it will be Auto save, then three manual save slots". Corrected: "If we can implement an autosave, let's do that. For manual save slots let's say 3; the top one will be the autosave slot and there will only be one. So it will be Auto save, then three manual save slots."
- **Built (Core, `SaveSlots`):** four slots in list order `Auto`, `Manual1..3` ("Auto save", "Save 1", "Save 2", "Save 3"), one file each (`autosave.json`, `save-1.json` to `save-3.json`) in the `WarInLocked` save folder. `SaveSlots.List()` gives the four rows for the menu (empty, loadable, or damaged with its error; name, level, map and saved time), `Latest()` gives the newest for a Continue button, `Save`, `Load`, `Delete`, `Autosave`. A save point writes a manual slot the player picks (`SavePoints.Interact(c, map, x, y, slot, out notice)`); the autosave slot is never written by hand.
- **PLACEHOLDERS:** an old single `save.json` is moved to Save 1 on first look (unless Save 1 already exists); saving over a used slot just overwrites (no "Are you sure?" yet); the autosave is skipped while fallen (in combat is allowed); a failed autosave is silent.
- **OPEN, Dyllan: when does the autosave happen?** Not said. Suggestions: when changing map, after a quest hand-in or level-up, and on a timer (every few minutes out of combat). Core only offers `SaveSlots.Autosave(...)`; nothing calls it yet.
- **Mapmaker:** the Load game list (Auto save on top, then Save 1 to 3, with empty and damaged rows), the slot picker at a save point, the autosave calls, and `MainMenuScreen` should use `SaveSlots.AnyExists()` / `SaveSlots.Latest()` instead of `SaveGame.Exists()` / `SaveGame.TryLoad(null, ...)`.
