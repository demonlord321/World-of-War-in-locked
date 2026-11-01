using System;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Combat
{
    /// <summary>
    /// Damage rules for physical (#14) and magical (#16) attacks, with separate weapon bonuses (#22).
    /// <code>
    /// physical base = 10 + main-hand Physical Attack        damage = base x StatModifier(Strength)
    /// magical  base = 10 + main-hand Magical Attack         damage = base x StatModifier(Intelligence)
    /// </code>
    /// An ability adds its power n on top of the base (Slash: physical base + n; Energy Ball: magical base + n).
    /// The result is multiplied by the target's resistance multiplier (#24, 1.0 if none) and <b>rounded half up to a whole number</b>
    /// once at the very end (x.5 goes up; 10.5 -> 11). Computed in exact decimals, so 0.5 never drifts.
    /// The Death debuff (#35) multiplies the result like a resistance: x0.75 / 0.5 / 0.25 (still rounded once at the end).
    /// Defence (#15) is not part of this yet.
    /// </summary>
    public static class DamageCalculator
    {
        public const int BaseAttack = 10;

        /// <summary>Strength modifier for the character, e.g. 2.3 at Str 13.</summary>
        public static double StrengthModifier(Character c) => StatModifier.Value(c.EffectiveStats.Strength);
        /// <summary>Intelligence modifier for the character.</summary>
        public static double IntelligenceModifier(Character c) => StatModifier.Value(c.EffectiveStats.Intelligence);

        /// <summary>True if the main hand holds a weapon (any weapon: sword or staff).</summary>
        public static bool HasWeapon(Character c)
        {
            var weapon = c.Equipment.Get(EquipmentSlot.MainHand);
            return weapon != null && weapon.Type == ItemType.Weapon;
        }

        /// <summary>The Physical Attack bonus of the weapon in the main hand (0 when unarmed or holding a non-weapon).</summary>
        public static int PhysicalAttackBonus(Character c)
        {
            var weapon = c.Equipment.Get(EquipmentSlot.MainHand);
            return weapon != null && weapon.Type == ItemType.Weapon ? weapon.PhysicalAttack : 0;
        }

        /// <summary>The Magical Attack bonus of the weapon in the main hand (0 when unarmed or holding a non-weapon).</summary>
        public static int MagicalAttackBonus(Character c)
        {
            var weapon = c.Equipment.Get(EquipmentSlot.MainHand);
            return weapon != null && weapon.Type == ItemType.Weapon ? weapon.MagicalAttack : 0;
        }

        /// <summary>Physical base attack: 10 + main-hand Physical Attack (15 with the Practice Sword).</summary>
        public static int PhysicalBaseAttack(Character c) => BaseAttack + PhysicalAttackBonus(c);

        /// <summary>Magical base attack: 10 + main-hand Magical Attack (15 with the Practice Staff).</summary>
        public static int MagicalBaseAttack(Character c) => BaseAttack + MagicalAttackBonus(c);

        /// <summary>Physical damage of one basic attack with whatever is in the main hand.</summary>
        public static int PhysicalDamage(Character c, double targetMultiplier = 1.0) => PhysicalDamage(c, PhysicalAttackBonus(c), targetMultiplier);

        /// <summary>Physical damage with an explicit bonus on top of the base 10 (weapon bonus + ability power) and the target's multiplier.</summary>
        public static int PhysicalDamage(Character c, int bonus, double targetMultiplier = 1.0) =>
            Compute(bonus, StatModifier.Tenths(c.EffectiveStats.Strength), targetMultiplier * c.DeathDebuffMultiplier);

        /// <summary>Unarmed damage: base attack x Strength modifier (ignores anything held).</summary>
        public static int UnarmedDamage(Character c) => PhysicalDamage(c, 0, 1.0);

        /// <summary>Magic damage with an explicit bonus on top of the base 10 (staff bonus + spell power) and the target's multiplier.</summary>
        public static int MagicDamage(Character c, int bonus = 0, double targetMultiplier = 1.0) =>
            Compute(bonus, StatModifier.Tenths(c.EffectiveStats.Intelligence), targetMultiplier * c.DeathDebuffMultiplier);

        private static int Compute(int bonus, int modifierTenths, double targetMultiplier)
        {
            if (bonus < 0) throw new ArgumentOutOfRangeException(nameof(bonus));
            if (targetMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(targetMultiplier));
            decimal raw = (BaseAttack + bonus) * (modifierTenths / 10m) * (decimal)targetMultiplier;
            return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }
    }
}
