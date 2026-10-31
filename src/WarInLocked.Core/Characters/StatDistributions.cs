using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>
    /// Bonus tables for each stat distribution (issue #3) and the race placeholder hook.
    /// Numbers are Dyllan's. Every distribution totals +15.
    /// </summary>
    public static class StatDistributions
    {
        /// <summary>Every stat starts here before any bonus (issue #2).</summary>
        public const int BaseStat = 8;

        /// <summary>The starting stats: all <see cref="BaseStat"/>.</summary>
        public static StatBlock BaseStats => StatBlock.Uniform(BaseStat);

        // Parameter order: Str, Dex, Con, Wis, Int, Per, Cha, Luck.
        private static readonly StatBlock WarriorBonus = new StatBlock(5, 5, 5, -1, -1, 2, 0, 0);
        private static readonly StatBlock MysticBonus = new StatBlock(-1, 1, -3, 5, 5, 5, 3, 0);
        private static readonly StatBlock MerchantBonus = new StatBlock(0, 1, 1, 1, 1, 1, 5, 5);

        /// <summary>The bonuses (can be negative) a distribution adds to the base stats.</summary>
        public static StatBlock GetBonus(StatDistribution distribution)
        {
            switch (distribution)
            {
                case StatDistribution.Warrior: return WarriorBonus;
                case StatDistribution.Mystic: return MysticBonus;
                case StatDistribution.Merchant: return MerchantBonus;
                default: throw new ArgumentOutOfRangeException(nameof(distribution), distribution, null);
            }
        }

        /// <summary>
        /// Placeholder hook: race does not change stats yet, so every race returns all zeros.
        /// Fill this table in once the design for races is decided.
        /// </summary>
        public static StatBlock GetRaceModifier(Race race)
        {
            switch (race)
            {
                case Race.Human:
                case Race.Elf:
                case Race.Dwarf:
                    return StatBlock.Zero;
                default: throw new ArgumentOutOfRangeException(nameof(race), race, null);
            }
        }

        /// <summary>Final stats = base 8 + distribution bonus + race modifier.</summary>
        public static StatBlock Calculate(Race race, StatDistribution distribution)
        {
            return BaseStats.Plus(GetBonus(distribution)).Plus(GetRaceModifier(race));
        }
    }
}
