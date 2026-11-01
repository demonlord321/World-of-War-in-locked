using System.Collections.Generic;

namespace WarInLocked.Core.Quests
{
    /// <summary>Whether an ability scales with Strength (physical) or Intelligence (magic).</summary>
    public enum AbilityKind { Physical, Magic }

    /// <summary>What an ability is and costs (#11, #20). Numbers are placeholders, see QUESTIONS.md.</summary>
    public sealed class AbilityDefinition
    {
        public AbilityDefinition(string id, string name, AbilityKind kind, string description, int basePower, int energyCost, double cooldownSeconds, bool requiresWeapon = false)
        {
            RequiresWeapon = requiresWeapon;
            Id = id;
            Name = name;
            Kind = kind;
            Description = description;
            BasePower = basePower;
            EnergyCost = energyCost;
            CooldownSeconds = cooldownSeconds;
        }

        public string Id { get; }
        public string Name { get; }
        public AbilityKind Kind { get; }
        /// <summary>One line for the abilities screen, e.g. "Cut an arch with your blade.".</summary>
        public string Description { get; }
        /// <summary>The power n at level 1. Physical damage is (10 + weapon bonus + n) x Strength modifier; magic is (10 + n) x Intelligence modifier.</summary>
        public int BasePower { get; }
        public int EnergyCost { get; }
        public double CooldownSeconds { get; }
        /// <summary>True if a weapon must be in the main hand to cast it (Slash). Unarmed casts are refused and cost nothing.</summary>
        public bool RequiresWeapon { get; }
        /// <summary>The reason shown when a weapon is missing.</summary>
        public const string NeedsWeaponText = "Needs a weapon";

        /// <summary>The power n at an ability level: BasePower + (level - 1). Slash: 5, 6, 7, ...</summary>
        public int PowerAt(int level) => BasePower + (level < 1 ? 0 : level - 1);
    }

    /// <summary>Ability ids, display names and definitions.</summary>
    public static class AbilityLibrary
    {
        public const string Slash = "slash";
        public const string EnergyBall = "energy-ball";

        /// <summary>Slash: physical, needs a weapon in the main hand, (10 + weapon bonus + n) x Strength modifier with n = 5 + (level - 1), 10 energy, 5 s cooldown.</summary>
        public static readonly AbilityDefinition SlashDefinition = new AbilityDefinition(
            Slash, "Slash", AbilityKind.Physical, "Cut an arch with your blade.", 5, 10, 5.0, requiresWeapon: true);

        /// <summary>Energy Ball (PLACEHOLDER): magic, (10 + n) x Intelligence modifier with the same n rule, 15 energy, 5 s cooldown.</summary>
        public static readonly AbilityDefinition EnergyBallDefinition = new AbilityDefinition(
            EnergyBall, "Energy Ball", AbilityKind.Magic, "Hurl a ball of raw energy.", 5, 15, 5.0);

        public static IReadOnlyList<AbilityDefinition> All { get; } = new[] { SlashDefinition, EnergyBallDefinition };

        /// <summary>The definition for an ability id, or null if it is unknown.</summary>
        public static AbilityDefinition Get(string id)
        {
            foreach (var d in All) if (d.Id == id) return d;
            return null;
        }

        public static string NameOf(string id) => Get(id)?.Name ?? id;
    }
}
