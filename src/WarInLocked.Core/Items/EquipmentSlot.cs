using System;

namespace WarInLocked.Core.Items
{
    /// <summary>
    /// The ten slots on the character. The numeric order is the order the UI draws them in the
    /// character menu (Head, Shoulder, Chest, Legs, Feet, Main Hand, Off Hand, Accessory 1-3), so <c>(int)slot</c> is a valid array index.
    /// </summary>
    public enum EquipmentSlot { Head, Shoulder, Chest, Legs, Feet, MainHand, OffHand, Accessory1, Accessory2, Accessory3 }

    public static class EquipmentSlots
    {
        public const int Count = 10;

        /// <summary>All slots in menu order.</summary>
        public static readonly EquipmentSlot[] All =
        {
            EquipmentSlot.Head, EquipmentSlot.Shoulder, EquipmentSlot.Chest, EquipmentSlot.Legs, EquipmentSlot.Feet,
            EquipmentSlot.MainHand, EquipmentSlot.OffHand, EquipmentSlot.Accessory1, EquipmentSlot.Accessory2, EquipmentSlot.Accessory3,
        };

        /// <summary>Display label, e.g. "Main Hand", "Accessory 1".</summary>
        public static string Label(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.MainHand: return "Main Hand";
                case EquipmentSlot.OffHand: return "Off Hand";
                case EquipmentSlot.Accessory1: return "Accessory 1";
                case EquipmentSlot.Accessory2: return "Accessory 2";
                case EquipmentSlot.Accessory3: return "Accessory 3";
                default: return slot.ToString();
            }
        }

        public static bool IsAccessory(EquipmentSlot slot) =>
            slot == EquipmentSlot.Accessory1 || slot == EquipmentSlot.Accessory2 || slot == EquipmentSlot.Accessory3;
    }
}
