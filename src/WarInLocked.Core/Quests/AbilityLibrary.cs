using System.Collections.Generic;

namespace WarInLocked.Core.Quests
{
    /// <summary>What an ability does (#11). Placeholder numbers, see QUESTIONS.md.</summary>
    public sealed class AbilityDefinition
    {
        public AbilityDefinition(string id, string name, double damageMultiplier, int energyCost, double cooldownSeconds)
        {
            Id = id;
            Name = name;
            DamageMultiplier = damageMultiplier;
            EnergyCost = energyCost;
            CooldownSeconds = cooldownSeconds;
        }

        public string Id { get; }
        public string Name { get; }
        /// <summary>Multiplies the character's basic attack damage.</summary>
        public double DamageMultiplier { get; }
        public int EnergyCost { get; }
        public double CooldownSeconds { get; }
    }

    /// <summary>Ability ids, display names and definitions.</summary>
    public static class AbilityLibrary
    {
        public const string Slash = "slash";

        /// <summary>Slash: 1.5x weapon damage, costs 10 energy, 2 second cooldown.</summary>
        public static readonly AbilityDefinition SlashDefinition = new AbilityDefinition(Slash, "Slash", 1.5, 10, 2.0);

        /// <summary>The definition for an ability id, or null if it is unknown.</summary>
        public static AbilityDefinition Get(string id) => id == Slash ? SlashDefinition : null;

        public static string NameOf(string id) => Get(id)?.Name ?? id;
    }
}
