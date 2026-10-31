using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>An immutable value for each of the eight stats.</summary>
    public sealed class StatBlock
    {
        private readonly int[] _values;

        public StatBlock(int strength, int dexterity, int constitution, int wisdom,
                         int intelligence, int perception, int charisma, int luck)
        {
            // Order must match StatType.
            _values = new[] { strength, dexterity, constitution, wisdom, intelligence, perception, charisma, luck };
        }

        private StatBlock(int[] values)
        {
            _values = values;
        }

        /// <summary>A block with every stat set to <paramref name="value"/>.</summary>
        public static StatBlock Uniform(int value)
        {
            var values = new int[StatTypes.Count];
            for (int i = 0; i < values.Length; i++) values[i] = value;
            return new StatBlock(values);
        }

        public static StatBlock Zero => Uniform(0);

        public int this[StatType stat] => _values[(int)stat];

        public int Strength => this[StatType.Strength];
        public int Dexterity => this[StatType.Dexterity];
        public int Constitution => this[StatType.Constitution];
        public int Wisdom => this[StatType.Wisdom];
        public int Intelligence => this[StatType.Intelligence];
        public int Perception => this[StatType.Perception];
        public int Charisma => this[StatType.Charisma];
        public int Luck => this[StatType.Luck];

        /// <summary>Sum of all eight values.</summary>
        public int Total
        {
            get
            {
                int sum = 0;
                foreach (int v in _values) sum += v;
                return sum;
            }
        }

        public StatBlock Plus(StatBlock other)
        {
            var values = new int[_values.Length];
            for (int i = 0; i < values.Length; i++) values[i] = _values[i] + other._values[i];
            return new StatBlock(values);
        }

        public override string ToString()
        {
            var parts = new string[_values.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = StatTypes.All[i] + " " + _values[i];
            return string.Join(", ", parts);
        }
    }
}
