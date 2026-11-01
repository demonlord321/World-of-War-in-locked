using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>
    /// The stacking Death debuff (#35, Dyllan): every respawn adds a level. Level 1 cuts max health, attack power (damage dealt) and max energy by 25% for 30 s;
    /// dying while it is active makes it level 2 (-50%, 60 s), then level 3 (-75%, 60 s, the maximum; no XP from kills while it is active, quest XP still counts).
    /// When the timer runs out the whole stack is removed. See <see cref="Character.DeathDebuffLevel"/>.
    /// </summary>
    public static class DeathDebuff
    {
        public const int MaxLevel = 3;
        /// <summary>Each level cuts this much (25%).</summary>
        public const double CutPerLevel = 0.25;
        /// <summary>Level 1 lasts 30 s.</summary>
        public const double FirstLevelSeconds = 30.0;
        /// <summary>Levels 2 and 3 last 60 s ("a minute").</summary>
        public const double StackedSeconds = 60.0;
        /// <summary>From this level on, kills give no XP (quest XP still counts).</summary>
        public const int NoKillXpLevel = 3;

        /// <summary>The stat multiplier for a level: 1.0, 0.75, 0.5, 0.25.</summary>
        public static double MultiplierFor(int level) => level <= 0 ? 1.0 : 1.0 - CutPerLevel * Math.Min(level, MaxLevel);

        /// <summary>The whole-percent cut for a level: 0, 25, 50, 75.</summary>
        public static int PercentFor(int level) => (int)Math.Round((1.0 - MultiplierFor(level)) * 100);

        /// <summary>How long a level lasts when it is (re)applied: 30 s for level 1, 60 s for 2 and 3; 0 for no debuff.</summary>
        public static double SecondsFor(int level) => level <= 0 ? 0 : level == 1 ? FirstLevelSeconds : StackedSeconds;

        /// <summary>"Death debuff 2", or null for level 0.</summary>
        public static string NameFor(int level) => level <= 0 ? null : "Death debuff " + Math.Min(level, MaxLevel);

        /// <summary>
        /// "Death debuff 2: -50% health, attack and energy (0:42)"; level 3 adds the XP rule:
        /// "Death debuff 3: -75% health, attack and energy, no XP from kills (0:42)". Null for level 0. The time is the remaining time rounded up to whole seconds, m:ss.
        /// </summary>
        public static string TextFor(int level, double remainingSeconds)
        {
            if (level <= 0) return null;
            int s = (int)Math.Ceiling(Math.Max(0, remainingSeconds));
            string time = (s / 60) + ":" + (s % 60).ToString("00");
            string xp = level >= NoKillXpLevel ? ", no XP from kills" : "";
            return NameFor(level) + ": -" + PercentFor(level) + "% health, attack and energy" + xp + " (" + time + ")";
        }
    }
}
