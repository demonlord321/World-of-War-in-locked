using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Combat
{
    /// <summary>
    /// The damage modifier a stat gives (#14, #16, changed by #46): <b>a flat 0.1 per stat point, with no milestone bonus</b>:
    /// 9 points = 0.9, 10 = 1.0, 25 = 2.5, 50 = 5.0.
    /// Dyllan, #46: "Ok let's leave it at 10% per point with no milestone jump. We can use something else for a milestone bonus".
    /// The milestone thresholds (10, 25, 50) stay defined in <see cref="Milestones"/> because a different milestone bonus will use them later
    /// (the title cap of #33 is gone: titles give their full bonus); <see cref="MilestoneTenths"/> is the bonus per milestone and is 0 for now.
    /// Everything is counted in tenths so there is no floating point error.
    /// </summary>
    public static class StatModifier
    {
        /// <summary>Tenths every stat point adds (0.1).</summary>
        public const int PointTenths = 1;
        /// <summary>Extra tenths added once for each milestone reached: 0 since #46 (no milestone bonus; set it to give milestones a bonus again).</summary>
        public const int MilestoneTenths = 0;

        private static IReadOnlyList<int> _milestones = new[] { 10, 25, 50 };

        /// <summary>The stat values that count as milestones. Change this list to retune the curve; it is read on every call.</summary>
        public static IReadOnlyList<int> Milestones
        {
            get => _milestones;
            set => _milestones = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>The modifier in tenths for a stat value, using <see cref="Milestones"/>. 9 -> 9, 10 -> 10, 25 -> 25.</summary>
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

        /// <summary>The smallest milestone above <paramref name="statPoints"/> (10 for 9, 25 for 10), or 0 if there is none left.</summary>
        public static int NextMilestone(int statPoints)
        {
            int next = 0;
            foreach (int m in _milestones)
                if (m > statPoints && (next == 0 || m < next)) next = m;
            return next;
        }

        /// <summary>The modifier as a number, e.g. 1.9.</summary>
        public static double Value(int statPoints) => Tenths(statPoints) / 10.0;
    }
}
