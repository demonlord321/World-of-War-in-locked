# Open questions

Questions for Dyllan. Answer them in the matching GitHub issue or tell the team in the room, and we'll update the code and tick them off here.

## 1. Does race change stats? (#2)

Human, Elf and Dwarf are choosable but have **no stat effect yet**. There is a placeholder table, `StatDistributions.GetRaceModifier`, that returns all zeros. If races should change stats, what are the numbers for each?

## 2. Is there a cap on stats? (#2)

8 is the starting value. Is there a maximum (for example 20), and can a stat go below some minimum? Nothing is capped in code right now.

## 3. Character name rules (#2)

Assumed for now: 1 to 20 characters, letters, digits, spaces, `'` and `-`. Is that fine?

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
- **Slimes** are level 1-3 at random per spawn: L1 100 HP / 10 attack, L2 125 / 12, L3 150 / 15. Enemies have separate Physical and Magical multipliers; slimes are vulnerable to both.

My interpretations and placeholders, please confirm:
- **Slime vulnerability = 1.25x to both physical and magical is a placeholder** (the number is mine). It applies to basic attacks, Slash and Energy Ball.
- **The multiplier is applied before rounding**, so rounding happens once (Warrior with sword: basic attack 15 x 2.3 x 1.25 = 43.125 -> 43; Slash 20 x 2.3 x 1.25 = 57.5 -> 58).
- **Slimes store their attack (10/12/15) but still do not fight back.** Is that right for now? Should slimes attack, and does the number mean damage per hit?
- **Slime level is uniform random 1-3** (a seeded `Random` can be passed in). **A respawned slime rolls a new level** (placeholder), after the usual 10 s.
- **Energy Ball does not need a staff** (placeholder): anyone who knows it can cast it. The staff only adds its Magical Attack to the base. Quest 3's text says "Equip your staff", but the quest counts casts with or without the staff equipped.
- **Quest 3 counts every Energy Ball cast that fires on an enemy** (a target next to you is required, like quest 2), whether or not it kills. Slash casts do not count.
- **The Practice Staff is given when you accept quest 3** (not when you turn in quest 2), matching how the sword is given at quest 1. It goes into the inventory and is **not equipped** (the sword stays equipped). **If the inventory is full**, accepting is refused with the notice "Your inventory is full", the quest stays Offered, and talking to the Colonel again re-offers it. The staff is only ever given once.
- **The speech is split into 3 dialogue boxes** at Dyllan's "..." pauses (the text joined is unchanged). Because quest 3 is offered straight after the speech, the same speech is repeated if you say No and talk to him again.
- **Placeholder Colonel lines for quest 3** (Dyllan gave none): while active "Keep at it, {name}... Energy ball 2/5."; turn-in "Good, {name}. You have a feel for magic now... Take this for your trouble."; after that he says the usual "done" line. The quest 2 turn-in filler lines ("Good, {name}. Practice is how you get strong...") are no longer used: they were replaced by the speech.
- **Slash with a staff** works (any weapon counts) and is (10 + 0 + n) x Strength modifier: the staff adds nothing physical. A basic attack with a staff equals an unarmed attack.
- **Copper** is the currency (starts at 0, never negative). Nothing spends it yet; it has no UI. Is "copper" the lowest coin, and are there silver and gold?

Hits to kill (level 1 abilities, starting stats, 1.25x slime vulnerability; sword Slash = (10+5+5) x Str, staff Energy Ball = (10+5+5) x Int):

| Class | Slash with sword | vs L1 / L2 / L3 | Energy Ball with staff | vs L1 / L2 / L3 |
|---|---|---|---|---|
| Warrior (Str 13, Int 7) | 58 | 2 / 3 / 3 | 18 | 6 / 7 / 9 |
| Mystic (Str 7, Int 13) | 18 | 6 / 7 / 9 | 58 | 2 / 3 / 3 |
| Merchant (Str 8, Int 9) | 20 | 5 / 7 / 8 | 23 | 5 / 6 / 7 |

Slimes are much tougher than the old 30 HP: a Warrior with the sword needs 3 basic attacks (43 each) for a level 1 slime, so quest 1 takes longer than before.
