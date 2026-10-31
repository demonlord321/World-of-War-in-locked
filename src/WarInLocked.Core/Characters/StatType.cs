using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>The eight character stats, in display order.</summary>
    public enum StatType
    {
        Strength,
        Dexterity,
        Constitution,
        Wisdom,
        Intelligence,
        Perception,
        Charisma,
        Luck,
    }

    public static class StatTypes
    {
        /// <summary>All stats in the order they are listed on screen.</summary>
        public static readonly StatType[] All =
        {
            StatType.Strength,
            StatType.Dexterity,
            StatType.Constitution,
            StatType.Wisdom,
            StatType.Intelligence,
            StatType.Perception,
            StatType.Charisma,
            StatType.Luck,
        };

        public static int Count => All.Length;
    }
}
