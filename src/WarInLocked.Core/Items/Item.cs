using System;

namespace WarInLocked.Core.Items
{
    /// <summary>An immutable item definition. Two items with the same <see cref="Id"/> are the same kind of item.</summary>
    public sealed class Item
    {
        /// <param name="equipSlot">Where the item can be worn, or null if it cannot be equipped.</param>
        /// <param name="baseDamage">For weapons: the damage of a basic attack before stat bonuses (#11).</param>
        /// <param name="maxStack">How many fit in one inventory slot. Equippable items must be 1.</param>
        public Item(string id, string name, ItemType type, EquipSlot? equipSlot = null, int maxStack = 1, int baseDamage = 0)
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
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            BaseDamage = baseDamage;
        }

        public string Id { get; }
        public string Name { get; }
        public ItemType Type { get; }
        public EquipSlot? EquipSlot { get; }
        public int MaxStack { get; }
        /// <summary>Base damage of a weapon (0 for everything else).</summary>
        public int BaseDamage { get; }

        /// <summary>True if the item has an <see cref="EquipSlot"/>. Whether it fits a specific slot is decided by <see cref="Equipment.CanEquip"/>.</summary>
        public bool IsEquippable => EquipSlot.HasValue;

        public override string ToString() => Name;
    }
}
