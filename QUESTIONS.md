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
- **Slash** (hotbar slot 1 once learned): 1.5x the physical damage, rounded half up once at the end (52 with the sword and Strength 13), costs 10 energy, 2 s cooldown. Using Slash also starts the 0.5 s attack delay. It does nothing if no slime is next to you, and then costs nothing. Is that right?
- **Energy** regenerates 2 per second (placeholder).

## 4b. Health and energy maximum (#12) - ANSWERED

Answered by Dyllan: **MaxHealth = 20 x Constitution** and **MaxEnergy = 5 x Wisdom**, with no rounding down and no minimum. Warrior (Con 13, Wis 7) = 260 health / 35 energy, Merchant (Con 9, Wis 9) = 180 / 45, Mystic (Con 5, Wis 13) = 100 / 65.

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
- **Health and energy:** when Constitution or Wisdom rises, the maximum goes up by 20 / 5 per point and the **current** value rises by the same amount (so a full bar stays full). Giving a point back lowers the current value too, but never kills the player (health stays at least 1). Levelling up by itself does **not** heal.
- XP is kept as a running total (`Xp`); the bar uses `XpIntoLevel / XpForNextLevel`. Extra XP carries over into the next level.

## 8. Physical and magical damage (#14, #16)

Dyllan's rules, as built:
- **Stat modifier** = 0.1 per stat point **plus 1.0 for every milestone reached**. Milestones are 10, 25 and 50 (a list, `StatModifier.Milestones`). So 9 -> 0.9, 10 -> 2.0, 11 -> 2.1, 25 -> 4.5, 50 -> 8.0.
- **Base attack** is 10 for everyone. **Physical damage** = (10 + weapon bonus) x Strength modifier. **Unarmed** = 10 x Strength modifier. **Slash** = 1.5 x that. **Magic damage** = (10 + spell power) x Intelligence modifier (no spells exist yet).
- **Rounding:** the result is rounded **half up** to a whole number (10.5 becomes 11), once at the very end (so Slash is not rounded twice).

Placeholders, please confirm:
- The weapon bonus is **added to the base 10 before multiplying** (not multiplied after). The **Practice Sword's bonus is 5** (so 15 x modifier).
- Defence (#15) is not part of the damage yet.

Hits to kill a 30 HP slime at the starting stats (Human, Strength 13/7/8):

| Class | Unarmed | Practice Sword | Slash (sword) |
|---|---|---|---|
| Warrior (Str 13 = 2.3) | 23 dmg, 2 hits | 35 dmg, 1 hit | 52 dmg, 1 hit |
| Mystic (Str 7 = 0.7) | 7 dmg, 5 hits | 11 dmg, 3 hits | 16 dmg, 2 hits |
| Merchant (Str 8 = 0.8) | 8 dmg, 4 hits | 12 dmg, 3 hits | 18 dmg, 2 hits |

A Warrior with the sword kills every slime in one hit, which makes the tutorial quick; Mystics and Merchants need 3. Should slimes have more HP?
