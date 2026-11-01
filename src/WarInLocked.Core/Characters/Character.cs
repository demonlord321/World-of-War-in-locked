using System;
using System.Collections.Generic;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Characters
{
    /// <summary>A created character. Build one with <see cref="CharacterCreation"/>.</summary>
    public sealed class Character
    {
        public Character(string name, Race race, StatDistribution distribution, StatBlock stats)
        {
            Name = name;
            Race = race;
            Distribution = distribution;
            Stats = stats;
            Inventory = new Inventory();
            Equipment = new Equipment(Inventory);
            Quests = new QuestLog(this);
            Combat = new CombatController(this);
            Health = MaxHealth;
            _energy = MaxEnergy;
        }

        /// <summary>The 10 inventory slots (#9).</summary>
        public Inventory Inventory { get; }
        /// <summary>The worn gear (#10); swaps with <see cref="Inventory"/>.</summary>
        public Equipment Equipment { get; }
        /// <summary>Quests offered, active and finished (#8).</summary>
        public QuestLog Quests { get; }

        /// <summary>Real-time combat rules, hotbar and cooldowns (#11). Call <c>Combat.Update(seconds)</c> every frame.</summary>
        public CombatController Combat { get; }

        /// <summary>Starts at 1 and rises as <see cref="Xp"/> grows (#13); see <see cref="Leveling"/>.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>XP gathered inside the current level (0 up to <see cref="XpForNextLevel"/> - 1).</summary>
        public int XpIntoLevel => (int)(Xp - Leveling.TotalXpFor(Level));
        /// <summary>XP that the current level needs in total before the next one.</summary>
        public int XpForNextLevel => Leveling.CostToNext(Level);
        /// <summary>0..1 for the XP bar.</summary>
        public float XpFraction => (float)XpIntoLevel / XpForNextLevel;

        /// <summary>Stat points earned and not yet spent.</summary>
        public int FreeStatPoints { get; private set; }
        private readonly int[] _assigned = new int[StatTypes.Count];
        /// <summary>How many points have been put into a stat so far (what <see cref="UnassignPoint"/> can take back).</summary>
        public int AssignedPoints(StatType stat) => _assigned[(int)stat];

        /// <summary>Raised once for every level gained, with the new level. Handy for a "Level up!" notice.</summary>
        public event Action<int> LeveledUp;
        /// <summary>Bumps on every level-up and every stat point change; poll it to refresh a UI.</summary>
        public int Version { get; private set; }

        /// <summary>
        /// Puts one free point into a stat. Returns false if there are none left.
        /// Constitution and Wisdom raise the maximum health/energy, and the current value rises by the same amount.
        /// </summary>
        public bool AssignPoint(StatType stat)
        {
            if (FreeStatPoints <= 0) return false;
            ChangeStat(stat, +1);
            FreeStatPoints--;
            Version++;
            return true;
        }

        /// <summary>Takes a previously assigned point back (never below the stat's creation value). Returns false if none was assigned there.</summary>
        public bool UnassignPoint(StatType stat)
        {
            if (_assigned[(int)stat] <= 0) return false;
            ChangeStat(stat, -1);
            FreeStatPoints++;
            Version++;
            return true;
        }

        private void ChangeStat(StatType stat, int delta)
        {
            int oldHealth = MaxHealth, oldEnergy = MaxEnergy;
            Stats = Stats.WithAdded(stat, delta);
            _assigned[(int)stat] += delta;
            int dh = MaxHealth - oldHealth, de = MaxEnergy - oldEnergy;
            if (delta > 0)
            {
                if (dh != 0 && Health > 0) Health += dh;
                _energy += de;
            }
            else
            {
                // Giving a point back lowers the current value too, but never kills and never drops below 1 health.
                if (dh != 0 && Health > 0) Health = Math.Max(1, Health + dh);
                _energy = Math.Max(0, _energy + de);
            }
            Health = Math.Min(Health, MaxHealth);
            _energy = Math.Min(_energy, MaxEnergy);
        }

        /// <summary>
        /// Max health = <c>HealthBase</c> x the Constitution modifier (0.1 per point, +1.0 per milestone; see <c>StatModifier</c>),
        /// rounded half up. Warrior (Con 13) = 230, Merchant (Con 9) = 90, Mystic (Con 5) = 50; Con 10 = 200, Con 25 = 450 (#15).
        /// </summary>
        public const int HealthBase = 100;
        /// <summary>Energy per point of Wisdom. Warrior (Wis 7) = 35, Merchant (Wis 9) = 45, Mystic (Wis 13) = 65.</summary>
        public const int EnergyPerWis = 5;

        public int MaxHealth => (int)Math.Round(HealthBase * (StatModifier.Tenths(Stats.Constitution) / 10m), MidpointRounding.AwayFromZero);
        public int MaxEnergy => EnergyPerWis * Stats.Wisdom;

        /// <summary>Current health, 0..<see cref="MaxHealth"/>. Starts full.</summary>
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;

        private double _energy;
        /// <summary>Current energy (whole points), 0..<see cref="MaxEnergy"/>. Starts full. Regenerates in <c>Combat.Update</c>.</summary>
        public int Energy => (int)Math.Floor(_energy);
        /// <summary>Energy including the fraction, for a smooth bar.</summary>
        public double EnergyExact => _energy;

        /// <summary>Removes health (not below 0). Returns the amount actually lost.</summary>
        public int TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int lost = Math.Min(amount, Health);
            Health -= lost;
            return lost;
        }

        public void Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Min(MaxHealth, Health + amount);
        }

        /// <summary>Spends energy if there is enough. Returns false (and spends nothing) otherwise.</summary>
        public bool SpendEnergy(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (_energy < amount) return false;
            _energy -= amount;
            return true;
        }

        public void RegenEnergy(double amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            _energy = Math.Min(MaxEnergy, _energy + amount);
        }

        public int Xp { get; private set; }
        private readonly List<string> _abilities = new List<string>();
        /// <summary>Ability ids learned so far, e.g. "slash".</summary>
        public IReadOnlyList<string> Abilities => _abilities;

        private readonly HashSet<string> _flags = new HashSet<string>();
        public bool HasFlag(string flag) => _flags.Contains(flag);
        public void SetFlag(string flag) => _flags.Add(flag);

        /// <summary>Adds XP (total). Can level up several times at once. Returns how many levels were gained; each one grants free stat points.</summary>
        public int GainXp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Xp += amount;
            int gained = 0;
            while (Xp >= Leveling.TotalXpFor(Level + 1))
            {
                Level++;
                gained++;
                FreeStatPoints += Leveling.StatPointsForReaching(Level);
                Version++;
                LeveledUp?.Invoke(Level);
            }
            return gained;
        }

        /// <summary>Learns an ability. Returns false if it was already known.</summary>
        public bool LearnAbility(string abilityId)
        {
            if (_abilities.Contains(abilityId)) return false;
            _abilities.Add(abilityId);
            return true;
        }

        public string Name { get; }
        public Race Race { get; }
        public StatDistribution Distribution { get; }
        /// <summary>Current stats: the creation values plus any assigned points.</summary>
        public StatBlock Stats { get; private set; }
    }
}
