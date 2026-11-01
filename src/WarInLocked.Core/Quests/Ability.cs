using System;

namespace WarInLocked.Core.Quests
{
    /// <summary>
    /// A learned ability and how practised it is (#20). Level 1 on learning; every cast that fires adds one use.
    /// Level 1 to 2 takes 5 uses, then 10, 20, 40, ... (doubling), counted <b>within the current level</b>
    /// (the counter restarts at 0 on each level up). Lives on <see cref="Characters.Character.LearnedAbilities"/>.
    /// </summary>
    public sealed class Ability
    {
        /// <summary>Uses needed to go from level 1 to level 2.</summary>
        public const int BaseUsesToLevel = 5;

        private int _usesIntoLevel;

        public Ability(AbilityDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Level = 1;
        }

        public AbilityDefinition Definition { get; }
        public string Id => Definition.Id;
        public string Name => Definition.Name;
        public string Description => Definition.Description;
        public AbilityKind Kind => Definition.Kind;

        public int Level { get; private set; }
        /// <summary>Every cast that fired, over all levels.</summary>
        public int Uses { get; private set; }
        /// <summary>Casts since the last level up (0 right after levelling).</summary>
        public int UsesIntoLevel => _usesIntoLevel;
        /// <summary>Casts needed in this level to reach the next one: 5, 10, 20, 40, ...</summary>
        public int UsesForNextLevel => UsesForLevel(Level);
        /// <summary>UsesIntoLevel / UsesForNextLevel, 0..1, for a progress bar.</summary>
        public double Fraction => (double)_usesIntoLevel / UsesForNextLevel;
        /// <summary>The power n at the current level (Slash: 5 at level 1, 6 at level 2, ...).</summary>
        public int Power => Definition.PowerAt(Level);

        /// <summary>Goes up on every use and every level up, so a screen can redraw only when it changed.</summary>
        public int Version { get; private set; }
        /// <summary>Raised after a level up, with this ability (already at the new level).</summary>
        public event Action<Ability> LeveledUp;

        /// <summary>Uses needed to leave a level: 5 x 2^(level - 1), capped to stay inside an int.</summary>
        public static int UsesForLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 28) level = 28;
            return BaseUsesToLevel * (1 << (level - 1));
        }

        /// <summary>Counts one cast that fired. Returns true if it caused a level up.</summary>
        public bool RecordUse()
        {
            Uses++;
            _usesIntoLevel++;
            Version++;
            if (_usesIntoLevel < UsesForNextLevel) return false;
            Level++;
            _usesIntoLevel = 0;
            LeveledUp?.Invoke(this);
            return true;
        }
    }
}
