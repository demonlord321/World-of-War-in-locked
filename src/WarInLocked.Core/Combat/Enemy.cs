using System;
using WarInLocked.Core.Quests;
using WarInLocked.Core.World;

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
        /// <summary>Slimes in the Forest of Beginnings are level 1 to 3 (#24). These are the defaults of <see cref="NewRandomSlime(Random)"/>.</summary>
        public const int MinSlimeLevel = 1, MaxSlimeLevel = 3;
        /// <summary>Slimes of the filler Path map between the forest and the encampment are level 4 to 5 (PLACEHOLDER, Dyllan 1 Nov).</summary>
        public const int PathMinSlimeLevel = 4, PathMaxSlimeLevel = 5;
        /// <summary>The slime levels that exist at all: 1 to 15 (levels above 5 are PLACEHOLDERS so night slimes, base level + 10, have stats).</summary>
        public const int LowestSlimeLevel = 1, HighestSlimeLevel = 15;
        /// <summary>At night (7 pm to 8 am, #57) monsters are this many levels stronger than their base level (Dyllan: "ten levels stronger").</summary>
        public const int NightLevelBonus = 10;
        /// <summary>The attack cooldown: after an attack an aggro'd enemy waits this long before the next one (Dyllan: 6 s, was 3).
        /// The first attack has no wait: it lands as soon as the player is in reach.</summary>
        public const double AttackIntervalSeconds = 6.0;
        /// <summary>PLACEHOLDER pace of an angered slime chasing the player: one tile every this many seconds (#31).</summary>
        public const double ChaseStepSeconds = 0.5;
        /// <summary>How long <see cref="AttackFlash"/> lasts after an attack, in seconds.</summary>
        public const double AttackFlashDuration = 0.25;
        /// <summary>How long the hit flicker lasts, in seconds.</summary>
        public const double FlashDuration = 0.25;

        /// <param name="level">Shown level (slimes 1-3).</param>
        /// <param name="attack">Attack value (slimes: damage dealt to the player every 6 s once aggro'd).</param>
        /// <param name="physicalMultiplier">Multiplier on physical damage taken: 1 normal, below 1 resistant, above 1 vulnerable.</param>
        /// <param name="magicalMultiplier">Multiplier on magical damage taken.</param>
        public Enemy(string type, int maxHp, int level = 1, int attack = 0, double physicalMultiplier = 1.0, double magicalMultiplier = 1.0)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            if (attack < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            if (physicalMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(physicalMultiplier));
            if (magicalMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(magicalMultiplier));
            Level = level;
            BaseLevel = level;
            Attack = attack;
            PhysicalMultiplier = physicalMultiplier;
            MagicalMultiplier = magicalMultiplier;
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("An enemy needs a type.", nameof(type));
            if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
            Type = type;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        /// <summary>
        /// Max health of a slime of this level: 50, 75, 100 (#24, Dyllan); PLACEHOLDER from level 4: 125, 150, ... the pattern continues
        /// at +25 HP per level (level 15 = 400).
        /// </summary>
        public static int SlimeHealthFor(int level) { CheckLevel(level); return 25 + 25 * level; }
        /// <summary>
        /// Attack of a slime of this level: 10, 12, 15 (#24, Dyllan); PLACEHOLDER from level 4: 18, 21, ... +3 per level (level 15 = 51).
        /// </summary>
        public static int SlimeAttackFor(int level) { CheckLevel(level); return level == 1 ? 10 : level == 2 ? 12 : 15 + 3 * (level - 3); }

        private static void CheckLevel(int level)
        {
            if (level < LowestSlimeLevel || level > HighestSlimeLevel) throw new ArgumentOutOfRangeException(nameof(level), "Slimes are level 1 to 15.");
        }

        /// <summary>
        /// The level a monster spawns at: its base level, plus <see cref="NightLevelBonus"/> at night (#57). Use the game clock of the character.
        /// PLACEHOLDER rule: slimes already alive keep their level; only new spawns and respawns use the time of the spawn.
        /// </summary>
        public static int LevelFor(int baseLevel, GameClock clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            return baseLevel + (clock.IsNight ? NightLevelBonus : 0);
        }

        /// <summary>A slime of base level <paramref name="baseLevel"/> (1-5), with the night bonus if it is night on the clock. <see cref="BaseLevel"/> keeps the base.</summary>
        public static Enemy NewSlime(int baseLevel, GameClock clock)
        {
            var e = NewSlime(LevelFor(baseLevel, clock));
            e.BaseLevel = baseLevel;
            return e;
        }

        /// <summary>
        /// The level the monster has by day (equals <see cref="Level"/> unless it spawned at night). <see cref="IsNightEmpowered"/> is true for night spawns:
        /// draw them with a night tint or similar if you like.
        /// </summary>
        public int BaseLevel { get; private set; }
        public bool IsNightEmpowered => Level > BaseLevel;

        /// <summary>A slime of the given level (1-15, default 1; pass a base level plus the clock to get the night bonus) with its health and attack. It takes standard damage (multipliers 1.0): Dyllan, 1 Nov, no weakness for now.</summary>
        public static Enemy NewSlime(int level = 1) =>
            new Enemy(SlimeType, SlimeHealthFor(level), level, SlimeAttackFor(level));

        /// <summary>A slime with a uniformly random level 1-3 (the forest). Pass a seeded <see cref="Random"/> to make it repeatable.</summary>
        public static Enemy NewRandomSlime(Random rng) => NewRandomSlime(rng, MinSlimeLevel, MaxSlimeLevel);

        /// <summary>
        /// A slime with a uniformly random level from <paramref name="minLevel"/> to <paramref name="maxLevel"/> (both included, 1-5): the level range is
        /// chosen per map, e.g. forest 1-3, Path 4-5 (<see cref="PathMinSlimeLevel"/>, <see cref="PathMaxSlimeLevel"/>).
        /// </summary>
        public static Enemy NewRandomSlime(Random rng, int minLevel, int maxLevel)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (minLevel < LowestSlimeLevel || maxLevel > HighestSlimeLevel || minLevel > maxLevel)
                throw new ArgumentOutOfRangeException(nameof(minLevel), "Slime levels are 1 to 15 and min must not exceed max.");
            return NewSlime(rng.Next(minLevel, maxLevel + 1));
        }

        /// <summary>
        /// Like <see cref="NewRandomSlime(Random,int,int)"/> but the random base level gets the night bonus when it is night on <paramref name="clock"/> (#57):
        /// forest 1-3 becomes 11-13 and the Path 4-5 becomes 14-15 at night. Call it for every new spawn and respawn.
        /// </summary>
        public static Enemy NewRandomSlime(Random rng, int minLevel, int maxLevel, GameClock clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (minLevel < LowestSlimeLevel || maxLevel > PathMaxSlimeLevel || minLevel > maxLevel)
                throw new ArgumentOutOfRangeException(nameof(minLevel), "Base slime levels are 1 to 5 and min must not exceed max.");
            return NewSlime(rng.Next(minLevel, maxLevel + 1), clock);
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
        /// <summary>Attack cooldown left in seconds while aggro'd: 0 = ready (0 at aggro, so the first hit is immediate), then <see cref="AttackIntervalSeconds"/> after each attack.</summary>
        public double AttackCooldownRemaining { get; private set; }
        /// <summary>Seconds of attack flash left (1 = just struck): <see cref="AttackFlashFraction"/> for a lunge/flicker.</summary>
        public double AttackFlash { get; private set; }
        public double AttackFlashFraction => AttackFlash / AttackFlashDuration;

        /// <summary>Makes the enemy hostile and ready (cooldown 0): it attacks the first time the player is in reach, then waits <see cref="AttackIntervalSeconds"/>. No effect if already aggro'd or dead.</summary>
        public void Provoke()
        {
            if (IsAggro || IsDead) return;
            IsAggro = true;
            AttackCooldownRemaining = 0;
        }

        /// <summary>
        /// Back to passive and fully healed (used when the player respawns, #29): no aggro, no attack timer, full HP, so dying
        /// again and again cannot wear a slime down. A dead enemy stays dead (the respawn queue brings it back as a new one).
        /// </summary>
        public void Calm()
        {
            IsAggro = false;
            AttackCooldownRemaining = 0;
            ChaseTimer = 0;
            if (!IsDead) Hp = MaxHp;
        }

        /// <summary>Counts down the cooldown; returns true when an attack lands now (cooldown 0 and the target in reach, checked on every attack).
        /// Out of reach nothing happens and the enemy stays ready (cooldown 0) so it hits as soon as the player is back in reach.</summary>
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

        /// <summary>Seconds spent walking toward the player since the last chase step (see <see cref="CombatController.UpdateEnemies"/>).</summary>
        internal double ChaseTimer { get; set; }

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
