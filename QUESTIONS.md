# Open questions

Questions for Dyllan. Answer them in the matching GitHub issue or tell the team in the room, and we'll update the code and tick them off here.

## 1. Mystic totals +17, the others +15 (#3)

The stat distribution bonuses add up to:

| Distribution | Bonus total |
|--------------|-------------|
| Warrior      | +15         |
| Mystic       | **+17**     |
| Merchant     | +15         |

The numbers are in the game exactly as written in #3. Is Mystic meant to be 2 points ahead, or should one stat come down by 2 (for example Per +5 -> +3)?

## 2. Does race change stats? (#2)

Human, Elf and Dwarf are choosable but have **no stat effect yet**. There is a placeholder table, `StatDistributions.GetRaceModifier`, that returns all zeros. If races should change stats, what are the numbers for each?

## 3. Is there a cap on stats? (#2)

8 is the starting value. Is there a maximum (for example 20), and can a stat go below some minimum? Nothing is capped in code right now.

## 4. Character name rules (#2)

Assumed for now: 1 to 20 characters, letters, digits, spaces, `'` and `-`. Is that fine?
