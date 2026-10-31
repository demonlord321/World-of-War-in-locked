using System;

namespace WarInLocked.Core.Characters
{
    /// <summary>
    /// The state of the character creator (issue #2): what the player has chosen so far.
    /// The UI edits this and reads <see cref="Stats"/> to update live.
    /// </summary>
    public sealed class CharacterCreation
    {
        public string Name { get; set; } = string.Empty;
        public Race Race { get; set; } = Race.Human;
        public StatDistribution Distribution { get; set; } = StatDistribution.Warrior;

        /// <summary>Final stats for the current choices.</summary>
        public StatBlock Stats => StatDistributions.Calculate(Race, Distribution);

        /// <summary>The distribution's bonus alone, for showing +/- next to each stat.</summary>
        public StatBlock Bonus => StatDistributions.GetBonus(Distribution);

        public NameError NameError => NameValidator.Validate(Name);

        /// <summary>True when Create may be pressed (the name is valid).</summary>
        public bool CanCreate => NameError == NameError.None;

        /// <summary>Builds the character. Throws if the name is not valid.</summary>
        public Character Create()
        {
            if (!CanCreate)
                throw new InvalidOperationException("Cannot create a character: name is " + NameError + ".");
            return new Character(NameValidator.Normalize(Name), Race, Distribution, Stats);
        }
    }
}
