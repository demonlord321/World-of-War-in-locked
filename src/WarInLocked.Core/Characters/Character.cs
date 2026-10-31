using System;

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
        }

        public string Name { get; }
        public Race Race { get; }
        public StatDistribution Distribution { get; }
        public StatBlock Stats { get; }
    }
}
