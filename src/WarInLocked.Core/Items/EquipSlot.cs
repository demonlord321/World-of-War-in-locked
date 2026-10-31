namespace WarInLocked.Core.Items
{
    /// <summary>
    /// Where an item is MADE to be worn (the item's own property). Items that cannot be equipped have no EquipSlot.
    /// <see cref="Accessory"/> fits any of the three accessory slots; see <see cref="EquipmentSlot"/> for the actual slots on the character.
    /// </summary>
    public enum EquipSlot { Head, Shoulder, Chest, Legs, Feet, MainHand, OffHand, Accessory }
}
