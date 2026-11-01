using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>
    /// Level-up rules (#13) in one place. Change the numbers here and everything else follows.
    ///
    /// XP cost from level L to L+1 is the previous cost plus a step, and the step depends on L:
    /// L 2-4 add 50 (so 50, 100, 150, 200), L 5-9 add 100 (300 ... 700), L 10 and up add 200,
    /// and that step doubles every <see cref="StepDoublesEvery"/> levels (L 20-29 add 400, L 30-39 add 800, ...).
    /// </summary>
    public static class Leveling
    {
        /// <summary>The cost from level 1 to level 2.</summary>
        public const int FirstLevelCost = 50;

        /// <summary>(first level, step) pairs. A level uses the last tier whose first level it has reached.</summary>
        private static readonly int[,] Tiers = { { 2, 50 }, { 5, 100 }, { 10, 200 } };

        /// <summary>From the last tier on, the step doubles every this many levels.</summary>
        public const int StepDoublesEvery = 10;

        /// <summary>Free stat points per level-up.</summary>
        public const int StatPointsPerLevel = 3;
        /// <summary>Free stat points when the level reached is a multiple of <see cref="BonusEvery"/> (instead of the normal amount). Placeholder: Dyllan may mean +5 extra.</summary>
        public const int StatPointsOnBonusLevel = 5;
        public const int BonusEvery = 5;

        /// <summary>How much more the cost from <paramref name="level"/> to the next level is than the cost from the level before it.</summary>
        public static int StepAt(int level)
        {
            if (level < 2) return 0;
            int lastTier = Tiers.GetLength(0) - 1;
            if (level < Tiers[lastTier, 0])
            {
                int i = lastTier;
                while (Tiers[i, 0] > level) i--;
                return Tiers[i, 1];
            }
            long step = Tiers[lastTier, 1];
            int doublings = (level - Tiers[lastTier, 0]) / StepDoublesEvery;
            for (int d = 0; d < doublings && step < int.MaxValue / 2; d++) step *= 2;
            return (int)step;
        }

        /// <summary>XP needed to go from <paramref name="level"/> to <c>level + 1</c>. Level 1 -> 50, 2 -> 100, 4 -> 200, 5 -> 300, 10 -> 900.</summary>
        public static int CostToNext(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            long cost = FirstLevelCost;
            for (int l = 2; l <= level; l++) cost += StepAt(l);
            return (int)Math.Min(cost, int.MaxValue);
        }

        /// <summary>Total XP needed to be at <paramref name="level"/> (0 for level 1).</summary>
        public static long TotalXpFor(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            long total = 0;
            for (int l = 1; l < level; l++) total += CostToNext(l);
            return total;
        }

        /// <summary>Free stat points granted for reaching <paramref name="level"/>.</summary>
        public static int StatPointsForReaching(int level) =>
            level > 1 && level % BonusEvery == 0 ? StatPointsOnBonusLevel : StatPointsPerLevel;
    }
}
