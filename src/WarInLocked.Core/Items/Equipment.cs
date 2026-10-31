using System;

namespace WarInLocked.Core.Items
{
    public enum EquipResult
    {
        /// <summary>Equipped. Any item that was in the slot is now back in the inventory (in the freed inventory slot).</summary>
        Success,
        /// <summary>The inventory index was out of range or empty.</summary>
        NoItem,
        /// <summary>The item has no EquipSlot (it is not equippable).</summary>
        NotEquippable,
        /// <summary>The item is equippable but not in the requested slot (for example a helmet in Feet).</summary>
        WrongSlot,
        /// <summary>Only from <see cref="Equipment.AddAndEquip"/>: nowhere to put the item.</summary>
        InventoryFull,
    }

    public enum UnequipResult
    {
        Success,
        /// <summary>The slot was already empty.</summary>
        NothingEquipped,
        /// <summary>The inventory has no room for the item, so it stays equipped.</summary>
        InventoryFull,
    }

    /// <summary>
    /// The ten worn slots (issue #10), tied to the character's <see cref="Inventory"/>.
    /// Equipping moves the item out of the inventory (swapping with whatever was worn); unequipping moves it back.
    /// UIs call <see cref="Get"/> for each slot and listen to <see cref="Changed"/>.
    /// </summary>
    public sealed class Equipment
    {
        private readonly Item[] _worn = new Item[EquipmentSlots.Count];

        public Equipment(Inventory inventory)
        {
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public Inventory Inventory { get; }

        public event EventHandler Changed;

        /// <summary>Goes up by one on every change, for polling UIs.</summary>
        public int Version { get; private set; }

        /// <summary>The item worn in a slot, or null.</summary>
        public Item Get(EquipmentSlot slot) => _worn[(int)slot];

        public bool IsEmpty(EquipmentSlot slot) => _worn[(int)slot] == null;

        /// <summary>
        /// The slot rules. Head/Shoulder/Chest/Legs/Feet take Armour made for that slot. MainHand takes a Weapon made for the main hand.
        /// OffHand takes a Shield made for the off hand, or any Weapon. Accessory 1-3 take Accessories.
        /// </summary>
        public static bool CanEquip(Item item, EquipmentSlot slot)
        {
            if (item == null || !item.EquipSlot.HasValue) return false;
            var made = item.EquipSlot.Value;
            switch (slot)
            {
                case EquipmentSlot.Head: return item.Type == ItemType.Armour && made == EquipSlot.Head;
                case EquipmentSlot.Shoulder: return item.Type == ItemType.Armour && made == EquipSlot.Shoulder;
                case EquipmentSlot.Chest: return item.Type == ItemType.Armour && made == EquipSlot.Chest;
                case EquipmentSlot.Legs: return item.Type == ItemType.Armour && made == EquipSlot.Legs;
                case EquipmentSlot.Feet: return item.Type == ItemType.Armour && made == EquipSlot.Feet;
                case EquipmentSlot.MainHand: return item.Type == ItemType.Weapon && made == EquipSlot.MainHand;
                case EquipmentSlot.OffHand:
                    return (item.Type == ItemType.Shield && made == EquipSlot.OffHand) ||
                           (item.Type == ItemType.Weapon && (made == EquipSlot.MainHand || made == EquipSlot.OffHand));
                default: return item.Type == ItemType.Accessory && made == EquipSlot.Accessory;
            }
        }

        /// <summary>The slot an item goes to by default, or null if it is not equippable. Accessories pick the first empty accessory slot (else Accessory 1).</summary>
        public EquipmentSlot? DefaultSlotFor(Item item)
        {
            if (item == null || !item.EquipSlot.HasValue) return null;
            switch (item.EquipSlot.Value)
            {
                case EquipSlot.Head: return EquipmentSlot.Head;
                case EquipSlot.Shoulder: return EquipmentSlot.Shoulder;
                case EquipSlot.Chest: return EquipmentSlot.Chest;
                case EquipSlot.Legs: return EquipmentSlot.Legs;
                case EquipSlot.Feet: return EquipmentSlot.Feet;
                case EquipSlot.MainHand: return EquipmentSlot.MainHand;
                case EquipSlot.OffHand: return EquipmentSlot.OffHand;
                default:
                    foreach (var s in new[] { EquipmentSlot.Accessory1, EquipmentSlot.Accessory2, EquipmentSlot.Accessory3 })
                        if (IsEmpty(s)) return s;
                    return EquipmentSlot.Accessory1;
            }
        }

        /// <summary>
        /// Equips the item in an inventory slot (what "Enter on an inventory item" does). Without <paramref name="target"/> the
        /// default slot is used. If the slot is occupied the two swap: the old item takes the inventory slot the new one left.
        /// Nothing changes unless the result is <see cref="EquipResult.Success"/>.
        /// </summary>
        public EquipResult Equip(int inventoryIndex, EquipmentSlot? target = null)
        {
            if (inventoryIndex < 0 || inventoryIndex >= Inventory.Capacity) return EquipResult.NoItem;
            var stack = Inventory.Get(inventoryIndex);
            if (stack == null) return EquipResult.NoItem;

            var item = stack.Item;
            if (!item.IsEquippable) return EquipResult.NotEquippable;
            var slot = target ?? DefaultSlotFor(item).Value;
            if (!CanEquip(item, slot)) return EquipResult.WrongSlot;

            var old = _worn[(int)slot];
            Inventory.Replace(inventoryIndex, old == null ? null : new ItemStack(old));
            _worn[(int)slot] = item;
            NotifyChanged();
            return EquipResult.Success;
        }

        /// <summary>Moves a worn item back to the inventory. Fails with <see cref="UnequipResult.InventoryFull"/> (and changes nothing) if there is no room.</summary>
        public UnequipResult Unequip(EquipmentSlot slot)
        {
            var item = _worn[(int)slot];
            if (item == null) return UnequipResult.NothingEquipped;
            if (!Inventory.CanAdd(item)) return UnequipResult.InventoryFull;

            Inventory.Add(item);
            _worn[(int)slot] = null;
            NotifyChanged();
            return UnequipResult.Success;
        }

        /// <summary>
        /// Gives the player an item and wears it straight away (the quest sword). The item passes through the inventory, so it
        /// ends up worn and any previously worn item ends up in the inventory. If the inventory is full the item is worn directly
        /// when its default slot is empty, otherwise <see cref="EquipResult.InventoryFull"/> and nothing changes.
        /// </summary>
        public EquipResult AddAndEquip(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var slot = DefaultSlotFor(item);
            if (slot == null) return EquipResult.NotEquippable;
            if (!CanEquip(item, slot.Value)) return EquipResult.WrongSlot;

            if (Inventory.CanAdd(item))
            {
                Inventory.Add(item);
                int index = Inventory.IndexOf(item.Id);
                return Equip(index, slot);
            }
            if (IsEmpty(slot.Value))
            {
                _worn[(int)slot.Value] = item;
                NotifyChanged();
                return EquipResult.Success;
            }
            return EquipResult.InventoryFull;
        }

        private void NotifyChanged()
        {
            Version++;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
