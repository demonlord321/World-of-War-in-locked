using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Combat
{
    /// <summary>
    /// The damage modifier a stat gives (#14, #16): <b>0.1 per stat point, plus 1.0 for every milestone reached</b>
    /// (the milestone's own 0.1 still counts). With milestones 10, 25 and 50:
    /// 9 points = 0.9, 10 = 2.0, 11 = 2.1, 25 = 4.5, 50 = 8.0.
    /// Everything is counted in tenths so there is no floating point error.
    /// </summary>
    public static class StatModifier
    {
        /// <summary>Tenths every stat point adds (0.1).</summary>
        public const int PointTenths = 1;
        /// <summary>Extra tenths added once for each milestone reached (1.0).</summary>
        public const int MilestoneTenths = 10;

        private static IReadOnlyList<int> _milestones = new[] { 10, 25, 50 };

        /// <summary>The stat values that count as milestones. Change this list to retune the curve; it is read on every call.</summary>
        public static IReadOnlyList<int> Milestones
        {
            get => _milestones;
            set => _milestones = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>The modifier in tenths for a stat value, using <see cref="Milestones"/>. 9 -> 9, 10 -> 20, 25 -> 45.</summary>
        public static int Tenths(int statPoints) => Tenths(statPoints, _milestones);

        /// <summary>The modifier in tenths with an explicit milestone list (each milestone counted once, however often it is listed).</summary>
        public static int Tenths(int statPoints, IEnumerable<int> milestones)
        {
            if (statPoints <= 0) return 0;
            int tenths = statPoints * PointTenths;
            var seen = new HashSet<int>();
            foreach (int m in milestones)
                if (m >= 1 && m <= statPoints && seen.Add(m)) tenths += MilestoneTenths;
            return tenths;
        }

        /// <summary>The modifier as a number, e.g. 1.9.</summary>
        public static double Value(int statPoints) => Tenths(statPoints) / 10.0;
    }
}
