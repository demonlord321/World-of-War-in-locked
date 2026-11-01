using System;
using WarInLocked.Core.Quests;

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
        /// <summary>Slimes in the Forest of Beginnings are level 1 to 3 (#24).</summary>
        public const int MinSlimeLevel = 1, MaxSlimeLevel = 3;
        /// <summary>PLACEHOLDER: slimes take 1.25x from both physical and magical damage ("vulnerable to both", #24).</summary>
        public const double SlimeVulnerability = 1.25;
        /// <summary>Aggro'd enemies attack the player this often (Dyllan: every 3 s).</summary>
        public const double AttackIntervalSeconds = 3.0;
        /// <summary>How long <see cref="AttackFlash"/> lasts after an attack, in seconds.</summary>
        public const double AttackFlashDuration = 0.25;
        /// <summary>How long the hit flicker lasts, in seconds.</summary>
        public const double FlashDuration = 0.25;

        /// <param name="level">Shown level (slimes 1-3).</param>
        /// <param name="attack">Attack value (slimes: damage dealt to the player every 3 s once aggro'd).</param>
        /// <param name="physicalMultiplier">Multiplier on physical damage taken: 1 normal, below 1 resistant, above 1 vulnerable.</param>
        /// <param name="magicalMultiplier">Multiplier on magical damage taken.</param>
        public Enemy(string type, int maxHp, int level = 1, int attack = 0, double physicalMultiplier = 1.0, double magicalMultiplier = 1.0)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            if (attack < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            if (physicalMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(physicalMultiplier));
            if (magicalMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(magicalMultiplier));
            Level = level;
            Attack = attack;
            PhysicalMultiplier = physicalMultiplier;
            MagicalMultiplier = magicalMultiplier;
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("An enemy needs a type.", nameof(type));
            if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
            Type = type;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        /// <summary>Max health of a slime of this level: 50, 75, 100 (#24, Dyllan; first 100/125/150, changed 1 Nov).</summary>
        public static int SlimeHealthFor(int level) => Clamp(level, 50, 75, 100);
        /// <summary>Attack of a slime of this level: 10, 12, 15 (#24, Dyllan). Stored only; slimes do not fight back yet.</summary>
        public static int SlimeAttackFor(int level) => Clamp(level, 10, 12, 15);

        private static int Clamp(int level, int l1, int l2, int l3)
        {
            if (level < MinSlimeLevel || level > MaxSlimeLevel) throw new ArgumentOutOfRangeException(nameof(level), "Slimes are level 1 to 3.");
            return level == 1 ? l1 : level == 2 ? l2 : l3;
        }

        /// <summary>A slime of the given level (1-3, default 1) with its health, attack and the placeholder vulnerability to both damage types.</summary>
        public static Enemy NewSlime(int level = 1) =>
            new Enemy(SlimeType, SlimeHealthFor(level), level, SlimeAttackFor(level), SlimeVulnerability, SlimeVulnerability);

        /// <summary>A slime with a uniformly random level 1-3. Pass a seeded <see cref="Random"/> to make it repeatable.</summary>
        public static Enemy NewRandomSlime(Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            return NewSlime(rng.Next(MinSlimeLevel, MaxSlimeLevel + 1));
        }

        public int Level { get; }
        /// <summary>Attack value (10 / 12 / 15 for slimes). Nothing uses it yet: enemies do not fight back.</summary>
        public int Attack { get; }
        /// <summary>Multiplier on physical damage taken (1.25 = vulnerable, 1 = normal, 0.5 = resistant).</summary>
        public double PhysicalMultiplier { get; }
        /// <summary>Multiplier on magical damage taken.</summary>
        public double MagicalMultiplier { get; }
        /// <summary>The multiplier for a kind of damage.</summary>
        public double MultiplierFor(AbilityKind kind) => kind == AbilityKind.Magic ? MagicalMultiplier : PhysicalMultiplier;
        /// <summary>"Vulnerable", "Resistant" or "Normal" for a kind of damage, for a monster info panel.</summary>
        public string ResistanceLabel(AbilityKind kind)
        {
            double m = MultiplierFor(kind);
            return m > 1.0 ? "Vulnerable" : m < 1.0 ? "Resistant" : "Normal";
        }
        /// <summary>Same as <see cref="MaxHp"/>.</summary>
        public int MaxHealth => MaxHp;

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

        /// <summary>
        /// False until the player damages this enemy (enemies are passive); true from then until it dies. A respawn is a new Enemy,
        /// so aggro starts over. Draw an "angry" state while true.
        /// </summary>
        public bool IsAggro { get; private set; }
        /// <summary>Seconds until the next attack while aggro'd (counts down from <see cref="AttackIntervalSeconds"/> at aggro).</summary>
        public double AttackCooldownRemaining { get; private set; }
        /// <summary>Seconds of attack flash left (1 = just struck): <see cref="AttackFlashFraction"/> for a lunge/flicker.</summary>
        public double AttackFlash { get; private set; }
        public double AttackFlashFraction => AttackFlash / AttackFlashDuration;

        /// <summary>Makes the enemy hostile; the first attack comes <see cref="AttackIntervalSeconds"/> later. No effect if already aggro'd or dead.</summary>
        public void Provoke()
        {
            if (IsAggro || IsDead) return;
            IsAggro = true;
            AttackCooldownRemaining = AttackIntervalSeconds;
        }

        /// <summary>Counts down the attack timer; returns true when an attack lands now. The timer waits (at 0) while the target is out of reach.</summary>
        internal bool TickAttack(double seconds, bool targetInReach)
        {
            if (AttackFlash > 0) AttackFlash = Math.Max(0, AttackFlash - seconds);
            if (!IsAggro || IsDead) return false;
            AttackCooldownRemaining = Math.Max(0, AttackCooldownRemaining - seconds);
            if (AttackCooldownRemaining > 0 || !targetInReach) return false;
            AttackCooldownRemaining = AttackIntervalSeconds;
            AttackFlash = AttackFlashDuration;
            return true;
        }

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
            if (dealt > 0 && !IsDead) Provoke();
            if (IsDead) IsAggro = false;
            return dealt;
        }

        public void Update(double seconds)
        {
            Age += seconds;
            if (DamageFlash > 0) DamageFlash = Math.Max(0, DamageFlash - seconds);
        }
    }
}
