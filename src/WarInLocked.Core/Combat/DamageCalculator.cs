using System;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Combat
{
    /// <summary>
    /// Damage rules for physical (#14) and magical (#16) attacks.
    /// <code>
    /// physical = (BaseAttack + weapon bonus) x StatModifier(Strength)     unarmed: BaseAttack x StatModifier(Strength)
    /// magical  = (BaseAttack + spell power)  x StatModifier(Intelligence)
    /// </code>
    /// BaseAttack is 10 for everyone. Equipment bonuses are added to the base before multiplying (PLACEHOLDER, see QUESTIONS.md).
    /// The result is <b>rounded half up to a whole number</b> (x.5 goes up; 10.5 -> 11). Computed in exact decimals, so 0.5 never drifts.
    /// Defence (#15) is not part of this yet.
    /// </summary>
    public static class DamageCalculator
    {
        public const int BaseAttack = 10;

        /// <summary>Strength modifier for the character, e.g. 2.3 at Str 13.</summary>
        public static double StrengthModifier(Character c) => StatModifier.Value(c.Stats.Strength);
        /// <summary>Intelligence modifier for the character.</summary>
        public static double IntelligenceModifier(Character c) => StatModifier.Value(c.Stats.Intelligence);

        /// <summary>The attack bonus of the weapon in the main hand (0 when unarmed or holding a non-weapon).</summary>
        public static int WeaponBonus(Character c)
        {
            var weapon = c.Equipment.Get(EquipmentSlot.MainHand);
            return weapon != null && weapon.Type == ItemType.Weapon ? weapon.BaseDamage : 0;
        }

        /// <summary>Physical damage of one basic attack with whatever is in the main hand.</summary>
        public static int PhysicalDamage(Character c) => PhysicalDamage(c, WeaponBonus(c), 1.0);

        /// <summary>Physical damage with an explicit weapon bonus and a multiplier (an ability's, 1.0 for a basic attack).</summary>
        public static int PhysicalDamage(Character c, int weaponBonus, double multiplier = 1.0) =>
            Compute(weaponBonus, StatModifier.Tenths(c.Stats.Strength), multiplier);

        /// <summary>Unarmed damage: base attack x Strength modifier (ignores anything held).</summary>
        public static int UnarmedDamage(Character c) => PhysicalDamage(c, 0, 1.0);

        /// <summary>Magic damage of a spell: (base attack + spell power) x Intelligence modifier. No spells exist yet.</summary>
        public static int MagicDamage(Character c, int spellPower = 0, double multiplier = 1.0) =>
            Compute(spellPower, StatModifier.Tenths(c.Stats.Intelligence), multiplier);

        private static int Compute(int bonus, int modifierTenths, double multiplier)
        {
            if (bonus < 0) throw new ArgumentOutOfRangeException(nameof(bonus));
            decimal raw = (BaseAttack + bonus) * (modifierTenths / 10m) * (decimal)multiplier;
            return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }
    }
}
