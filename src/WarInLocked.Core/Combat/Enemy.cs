using System;

namespace WarInLocked.Core.Combat
{
    /// <summary>
    /// The combat state of one enemy on the map (a slime for now). The screen keeps one per map creature and
    /// keeps <see cref="TileX"/>/<see cref="TileY"/> up to date; Core decides damage and death (#11).
    /// For drawing: <see cref="Hp"/>/<see cref="MaxHp"/> for a health bar, and <see cref="DamageFlash"/> for a hit flicker.
    /// </summary>
    public sealed class Enemy
    {
        /// <summary>The quest enemy type, e.g. "slime".</summary>
        public const string SlimeType = "slime";
        public const int SlimeMaxHp = 30;
        /// <summary>How long the hit flicker lasts, in seconds.</summary>
        public const double FlashDuration = 0.25;

        public Enemy(string type, int maxHp)
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("An enemy needs a type.", nameof(type));
            if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
            Type = type;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public static Enemy NewSlime() => new Enemy(SlimeType, SlimeMaxHp);

        public string Type { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;
        /// <summary>True after the first hit, until it dies. Draw the health bar only while this is true.</summary>
        public bool IsDamaged => Hp < MaxHp && Hp > 0;
        /// <summary>Hp as 0..1 for a bar.</summary>
        public double HpFraction => (double)Hp / MaxHp;

        /// <summary>Seconds of flicker left. Above 0 right after a hit, counting down in <see cref="Update"/>.
        /// <see cref="DamageFlashFraction"/> gives 1 (just hit) down to 0.</summary>
        public double DamageFlash { get; private set; }
        public double DamageFlashFraction => DamageFlash / FlashDuration;
        /// <summary>Total seconds this enemy has been alive (advanced by <see cref="Update"/>); <see cref="LastHitTime"/> uses the same clock.</summary>
        public double Age { get; private set; }
        /// <summary>The <see cref="Age"/> at the last hit, or -1 if never hit.</summary>
        public double LastHitTime { get; private set; } = -1;
        public int LastDamage { get; private set; }

        public int TileX { get; set; }
        public int TileY { get; set; }

        /// <summary>Applies damage (not below 0) and starts the flicker. Returns the damage actually dealt.</summary>
        public int TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsDead) return 0;
            int dealt = Math.Min(amount, Hp);
            Hp -= dealt;
            LastDamage = dealt;
            LastHitTime = Age;
            DamageFlash = FlashDuration;
            return dealt;
        }

        public void Update(double seconds)
        {
            Age += seconds;
            if (DamageFlash > 0) DamageFlash = Math.Max(0, DamageFlash - seconds);
        }
    }
}
