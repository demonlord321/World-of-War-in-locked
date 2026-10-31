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

        /// <summary>Always 1 for now: levelling is on hold (#12). XP just accumulates in <see cref="Xp"/>.</summary>
        public int Level => 1;

        /// <summary>100 per full 5 points of Constitution (rounded down), at least 100. Warrior (Con 13) = 200, Mystic (Con 5) = 100.</summary>
        public const int HealthPerFiveCon = 100;
        public const int MinHealth = 100;
        /// <summary>25 per full 5 points of Wisdom (rounded down), at least 25. Warrior (Wis 7) = 25, Mystic (Wis 13) = 50.</summary>
        public const int EnergyPerFiveWis = 25;
        public const int MinEnergy = 25;

        public int MaxHealth => Math.Max(MinHealth, HealthPerFiveCon * (Stats.Constitution / 5));
        public int MaxEnergy => Math.Max(MinEnergy, EnergyPerFiveWis * (Stats.Wisdom / 5));

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

        public void GainXp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Xp += amount;
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
        public StatBlock Stats { get; }
    }
}
