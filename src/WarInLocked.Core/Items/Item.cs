using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Items
{
    /// <summary>An immutable item definition. Two items with the same <see cref="Id"/> are the same kind of item.</summary>
    public sealed class Item
    {
        /// <param name="equipSlot">Where the item can be worn, or null if it cannot be equipped.</param>
        /// <param name="physicalAttack">Physical Attack bonus: added to the base attack of 10 before the Strength modifier when worn in the main hand (#14, #22).</param>
        /// <param name="magicalAttack">Magical Attack bonus: added to the base attack of 10 before the Intelligence modifier when worn in the main hand (#22).</param>
        /// <param name="description">Flavour text for the equipment detail screen.</param>
        /// <param name="healAmount">Consumables: health restored when used (#26), capped at the character's max health. 0 = no heal.</param>
        /// <param name="useCooldownSeconds">Consumables: the shared use cooldown that starts after using one (#26).</param>
        /// <param name="sellPriceCopper">What a shop pays for one (#36), 0 if it cannot be sold.</param>
        /// <param name="maxStack">How many fit in one inventory slot. 0 (the default) means the rule from Dyllan: equipment (anything equippable) never stacks (1), every other item stacks up to <see cref="DefaultStackSize"/>. An explicit value is only for special cases and tests; equippable items must be 1.</param>
        public Item(string id, string name, ItemType type, EquipSlot? equipSlot = null, int maxStack = 0, int physicalAttack = 0, int magicalAttack = 0, string description = "", int healAmount = 0, double useCooldownSeconds = 0, bool recoversVitals = false, int sellPriceCopper = 0)
        {
            if (sellPriceCopper < 0) throw new ArgumentOutOfRangeException(nameof(sellPriceCopper));
            SellPriceCopper = sellPriceCopper;
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An item needs an id.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("An item needs a name.", nameof(name));
            if (maxStack < 0) throw new ArgumentOutOfRangeException(nameof(maxStack));
            if (equipSlot.HasValue && maxStack > 1) throw new ArgumentException("Equippable items cannot stack.", nameof(maxStack));
            if (maxStack == 0) maxStack = equipSlot.HasValue ? 1 : DefaultStackSize;
            Id = id;
            Name = name;
            Type = type;
            EquipSlot = equipSlot;
            MaxStack = maxStack;
            if (physicalAttack < 0) throw new ArgumentOutOfRangeException(nameof(physicalAttack));
            if (magicalAttack < 0) throw new ArgumentOutOfRangeException(nameof(magicalAttack));
            PhysicalAttack = physicalAttack;
            MagicalAttack = magicalAttack;
            Description = description ?? "";
            if (healAmount < 0) throw new ArgumentOutOfRangeException(nameof(healAmount));
            if (useCooldownSeconds < 0) throw new ArgumentOutOfRangeException(nameof(useCooldownSeconds));
            RecoversVitals = recoversVitals;
            HealAmount = healAmount;
            UseCooldownSeconds = useCooldownSeconds;
        }

        /// <summary>What a shop pays for one, in copper (#36); 0 = cannot be sold. A loot item with no other use (Slime Jelly) exists to be sold.</summary>
        public int SellPriceCopper { get; }
        public bool IsSellable => SellPriceCopper > 0;
        public string Id { get; }
        public string Name { get; }
        public ItemType Type { get; }
        public EquipSlot? EquipSlot { get; }
        /// <summary>How many of a non-equipment item fit in one inventory slot: 99 (set by Dyllan, confirmed).</summary>
        public const int DefaultStackSize = 99;
        /// <summary>How many fit in one slot: 1 for equipment, <see cref="DefaultStackSize"/> for everything else (unless set explicitly).</summary>
        public int MaxStack { get; }
        /// <summary>True when more than one fits in a slot (every non-equipment item). The slot shows its count (<c>ItemStack.Count</c>) when this is true and the count is above 1.</summary>
        public bool IsStackable => MaxStack > 1;
        /// <summary>Physical Attack bonus (0 if none); see DamageCalculator.</summary>
        public int PhysicalAttack { get; }
        /// <summary>Magical Attack bonus (0 if none); see DamageCalculator.</summary>
        public int MagicalAttack { get; }
        /// <summary>Flavour text, "" if none.</summary>
        public string Description { get; }
        /// <summary>Health restored when used (0 if none).</summary>
        public int HealAmount { get; }
        /// <summary>Seconds of shared cooldown after using one (0 if none).</summary>
        public double UseCooldownSeconds { get; }
        /// <summary>True for items that can be used from the inventory or hotbar (type Consumable with an effect), see <c>Character.UseItem</c>.</summary>
        /// <summary>
        /// Rations-style recovery (#26): usable only out of combat; restores health AND energy to max up to level <see cref="FullRecoveryMaxLevel"/>,
        /// and <see cref="ReducedRecoveryFraction"/> of the max above that.
        /// </summary>
        public bool RecoversVitals { get; }
        public const int FullRecoveryMaxLevel = 10;
        public const double ReducedRecoveryFraction = 0.5;
        public bool IsUsable => Type == ItemType.Consumable && (HealAmount > 0 || RecoversVitals);

        /// <summary>What using the item does, ready to print, e.g. "Heals 50 HP." and "Cooldown after use 5s". Empty if it has no effect.</summary>
        public IReadOnlyList<string> EffectLines
        {
            get
            {
                var lines = new List<string>();
                if (HealAmount > 0) lines.Add("Heals " + HealAmount + " HP.");
                if (IsUsable && UseCooldownSeconds > 0) lines.Add("Cooldown after use " + UseCooldownSeconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "s");
                return lines;
            }
        }

        /// <summary>BonusLines then EffectLines: everything a detail screen prints under the description.</summary>
        public IReadOnlyList<string> DetailLines
        {
            get
            {
                var lines = new List<string>(BonusLines);
                lines.AddRange(EffectLines);
                return lines;
            }
        }

        /// <summary>The bonuses ready to print on a detail screen, e.g. "Physical Attack +5". Empty if the item has none.</summary>
        public IReadOnlyList<string> BonusLines
        {
            get
            {
                var lines = new List<string>();
                if (PhysicalAttack != 0) lines.Add("Physical Attack +" + PhysicalAttack);
                if (MagicalAttack != 0) lines.Add("Magical Attack +" + MagicalAttack);
                return lines;
            }
        }

        /// <summary>True if the item has an <see cref="EquipSlot"/>. Whether it fits a specific slot is decided by <see cref="Equipment.CanEquip"/>.</summary>
        public bool IsEquippable => EquipSlot.HasValue;

        public override string ToString() => Name;
    }
}
