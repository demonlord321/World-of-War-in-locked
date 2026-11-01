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
        /// <param name="maxStack">How many fit in one inventory slot. Equippable items must be 1.</param>
        public Item(string id, string name, ItemType type, EquipSlot? equipSlot = null, int maxStack = 1, int physicalAttack = 0, int magicalAttack = 0, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An item needs an id.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("An item needs a name.", nameof(name));
            if (maxStack < 1) throw new ArgumentOutOfRangeException(nameof(maxStack));
            if (equipSlot.HasValue && maxStack != 1) throw new ArgumentException("Equippable items cannot stack.", nameof(maxStack));
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
        }

        public string Id { get; }
        public string Name { get; }
        public ItemType Type { get; }
        public EquipSlot? EquipSlot { get; }
        public int MaxStack { get; }
        /// <summary>Physical Attack bonus (0 if none); see DamageCalculator.</summary>
        public int PhysicalAttack { get; }
        /// <summary>Magical Attack bonus (0 if none); see DamageCalculator.</summary>
        public int MagicalAttack { get; }
        /// <summary>Flavour text, "" if none.</summary>
        public string Description { get; }

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
