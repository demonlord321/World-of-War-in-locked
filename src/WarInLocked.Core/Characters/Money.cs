using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>
    /// Currency maths (#21): 100 copper = 1 silver, 100 silver = 1 gold. A character stores one total in copper
    /// (<see cref="Character.Copper"/>); this class splits it into gold / silver / copper for display.
    /// </summary>
    public static class Money
    {
        public const int CopperPerSilver = 100;
        public const int SilverPerGold = 100;
        public const int CopperPerGold = CopperPerSilver * SilverPerGold;   // 10000

        /// <summary>Whole gold coins in <paramref name="totalCopper"/> (0 or more, no upper limit).</summary>
        public static long Gold(long totalCopper) => Check(totalCopper) / CopperPerGold;

        /// <summary>The silver part, 0-99.</summary>
        public static int Silver(long totalCopper) => (int)(Check(totalCopper) % CopperPerGold / CopperPerSilver);

        /// <summary>The copper part, 0-99.</summary>
        public static int CopperPart(long totalCopper) => (int)(Check(totalCopper) % CopperPerSilver);

        /// <summary>Total copper for gold + silver + copper. All three must be 0 or more; silver and copper above 99 simply carry over (FromParts(0, 150, 0) = 15000).</summary>
        public static long FromParts(long gold, long silver, long copper)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (silver < 0) throw new ArgumentOutOfRangeException(nameof(silver));
            if (copper < 0) throw new ArgumentOutOfRangeException(nameof(copper));
            return gold * CopperPerGold + silver * CopperPerSilver + copper;
        }

        /// <summary>
        /// Text for the UI, e.g. "1g 23s 45c". Rules: units are written as number + g / s / c, separated by one space, highest first.
        /// Gold is shown only from 10000 copper up and silver only from 100 copper up (leading zero units are left out), but once a higher unit is shown
        /// every lower unit is shown too, even when it is 0 ("1g 0s 5c", "2s 0c"). Copper is always shown ("0c" for nothing, "50c").
        /// </summary>
        public static string Format(long totalCopper)
        {
            Check(totalCopper);
            long gold = Gold(totalCopper);
            int silver = Silver(totalCopper), copper = CopperPart(totalCopper);
            if (gold > 0) return gold + "g " + silver + "s " + copper + "c";
            if (silver > 0) return silver + "s " + copper + "c";
            return copper + "c";
        }

        private static long Check(long totalCopper)
        {
            if (totalCopper < 0) throw new ArgumentOutOfRangeException(nameof(totalCopper), "Money cannot be negative.");
            return totalCopper;
        }
    }
}
