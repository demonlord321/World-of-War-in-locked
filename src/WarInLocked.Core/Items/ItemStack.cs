using System;

namespace WarInLocked.Core.Items
{
    /// <summary>A number of identical items sitting in one inventory slot. Immutable.</summary>
    public sealed class ItemStack
    {
        public ItemStack(Item item, int count = 1)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (count < 1 || count > item.MaxStack)
                throw new ArgumentOutOfRangeException(nameof(count), "A stack holds between 1 and the item's MaxStack.");
            Item = item;
            Count = count;
        }

        public Item Item { get; }
        public int Count { get; }

        public override string ToString() => Count > 1 ? Item.Name + " x" + Count : Item.Name;
    }
}
