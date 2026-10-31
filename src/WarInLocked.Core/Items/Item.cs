using System;

namespace WarInLocked.Core.Items
{
    /// <summary>An immutable item definition. Two items with the same <see cref="Id"/> are the same kind of item.</summary>
    public sealed class Item
    {
        /// <param name="equipSlot">Where the item can be worn, or null if it cannot be equipped.</param>
        /// <param name="maxStack">How many fit in one inventory slot. Equippable items must be 1.</param>
        public Item(string id, string name, ItemType type, EquipSlot? equipSlot = null, int maxStack = 1)
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
        }

        public string Id { get; }
        public string Name { get; }
        public ItemType Type { get; }
        public EquipSlot? EquipSlot { get; }
        public int MaxStack { get; }

        /// <summary>True if the item has an <see cref="EquipSlot"/>. Whether it fits a specific slot is decided by <see cref="Equipment.CanEquip"/>.</summary>
        public bool IsEquippable => EquipSlot.HasValue;

        public override string ToString() => Name;
    }
}
