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
- **Slimes** have 30 HP (hits to kill: see entry 8). They do **not fight back yet**: should they? How much damage do they do, and should they respawn?
- **Slash** (hotbar slot 1 once learned): see entry 9 (#20): (10 + weapon bonus + n) x Strength modifier, 10 energy, 5 s cooldown, needs a weapon. Using Slash also starts the 0.5 s attack delay. It does nothing if no slime is next to you, and then costs nothing.
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
- **Base attack** is 10 for everyone. **Physical damage** = (10 + weapon bonus) x Strength modifier. **Unarmed** = 10 x Strength modifier. **Slash** = (10 + weapon bonus + n) x Strength modifier (entry 9). **Magic damage** = (10 + spell power) x Intelligence modifier (no spells exist yet).
- **Rounding:** the result is rounded **half up** to a whole number (10.5 becomes 11), once at the very end (so abilities are not rounded twice).

Placeholders, please confirm:
- The weapon bonus is **added to the base 10 before multiplying** (not multiplied after). The **Practice Sword's bonus is 5** (so 15 x modifier).
- Defence (#15) is not part of the damage yet.

Hits to kill a 30 HP slime at the starting stats (Human, Strength 13/7/8):

| Class | Unarmed | Practice Sword | Slash level 1 (sword) |
|---|---|---|---|
| Warrior (Str 13 = 2.3) | 23 dmg, 2 hits | 35 dmg, 1 hit | 46 dmg, 1 hit |
| Mystic (Str 7 = 0.7) | 7 dmg, 5 hits | 11 dmg, 3 hits | 14 dmg, 3 hits |
| Merchant (Str 8 = 0.8) | 8 dmg, 4 hits | 12 dmg, 3 hits | 16 dmg, 2 hits |

A Warrior with the sword kills every slime in one hit, which makes the tutorial quick; Mystics and Merchants need 3. Should slimes have more HP?

## 9. Abilities, levels and Tutorial Quest 2 (#19, #20)

Decided by Dyllan:
- **Slash** = round_half_up((BaseAttack + n) x Strength modifier), **BaseAttack = 10 + weapon bonus** (sword +5 = 15), **n = 5 + (level - 1)**. Level 1 Warrior with sword: (15 + 5) x 2.3 = 46. 10 energy, **5 s cooldown**. It replaces the old 1.5x rule.
- **Slash needs a weapon in the main hand.** Unarmed it is refused with the reason "Needs a weapon": no energy is spent, no cooldown starts, no use is counted.
- **Every cast that actually fires counts one use** (not no-target, cooldown, not-enough-energy or no-weapon casts).
- **Uses to level up:** 5 for level 1 -> 2, then 10, 20, 40 ... (doubling).
- **Energy Ball** (reward of quest 2): magic, (10 + n) x Intelligence modifier, same n rule, **15 energy, 5 s cooldown**. No weapon needed.
- **Tutorial:** after the 5th slime you must go back to Colonel Powers to turn the quest in; the 50 XP and Slash are given then (the sword stays). Quest 2 "Slashing Practice": "Use Slash 5 times on enemies.", progress "Slash used 0/5" (counts Slash casts that hit an enemy), reward Energy Ball + 50 XP, failure None.

My interpretations and placeholders, please confirm:
- **"5 uses, then 10, 20 ..." is counted within the current level**: UsesIntoLevel restarts at 0 on every level-up, so level 3 needs 15 uses in total (5 + 10), level 4 needs 35 (5 + 10 + 20). The total Uses is also kept. If you meant 10 total for level 3, it is a one-line change.
- Level 1 is the start; the table doubles forever (clamped around level 28 so numbers do not overflow). No maximum level.
- **Quest 2 is also turned in at Colonel Powers** (same as quest 1). His turn-in lines for quest 2 are placeholders ("Good, {name}. Practice is how you get strong... Take this, and use it well."), as are the lines while it is active ("Keep at it, {name}... Slash used 0/5.").
- If quest 2 is declined (No), the Colonel offers it again next time you talk to him, after repeating "You now have your first ability, why not test it out...".
- **Energy Ball is a placeholder**: its description ("Hurl a ball of raw energy."), base power 5, cost 15 and the 5 s cooldown follow the Slash pattern. Nothing else uses magic yet. The Warrior (Int 7) does (10 + 5) x 0.7 = 10.5 -> 11 per ball; the Mystic (Int 13) does 35.
- **Slimes respawn** at their original tile **10 s after dying** (placeholder delay; it waits while something is standing on the tile), so quest 2 can always be finished.
- Slash drops out of reach if the sword is unequipped mid-quest (the Colonel does not give a second one), so a player who sells or drops it cannot finish quest 2 until they get another weapon.
