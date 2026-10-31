using System;
using System.Collections.Generic;
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
        }

        /// <summary>The 10 inventory slots (#9).</summary>
        public Inventory Inventory { get; }
        /// <summary>The worn gear (#10); swaps with <see cref="Inventory"/>.</summary>
        public Equipment Equipment { get; }
        /// <summary>Quests offered, active and finished (#8).</summary>
        public QuestLog Quests { get; }

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
