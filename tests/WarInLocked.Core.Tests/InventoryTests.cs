using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

public class InventoryTests
{
    private static readonly Item Sword = new("sword", "Sword", ItemType.Weapon, EquipSlot.MainHand);
    private static readonly Item Potion = new("potion", "Potion", ItemType.Misc, maxStack: 5);
    private static readonly Item Rock = new("rock", "Rock", ItemType.Misc);

    [Fact]
    public void HasTenEmptySlotsByDefault()
    {
        var inv = new Inventory();
        Assert.Equal(10, inv.Capacity);
        Assert.Equal(10, inv.Slots.Count);
        Assert.All(inv.Slots, s => Assert.Null(s));
        Assert.Equal(10, inv.FreeSlots);
        Assert.False(inv.IsFull);
    }

    [Fact]
    public void AddPutsItemsInTheFirstFreeSlots()
    {
        var inv = new Inventory();
        Assert.Equal(1, inv.Add(Sword));
        Assert.Equal(1, inv.Add(Rock));
        Assert.Equal("Sword", inv.Get(0).Item.Name);
        Assert.Equal("Rock", inv.Get(1).Item.Name);
        Assert.Null(inv.Get(2));
    }

    [Fact]
    public void StackableItemsStackUpToMaxThenOverflowIntoNewSlots()
    {
        var inv = new Inventory();
        Assert.Equal(12, inv.Add(Potion, 12));
        Assert.Equal(5, inv.Get(0).Count);
        Assert.Equal(5, inv.Get(1).Count);
        Assert.Equal(2, inv.Get(2).Count);
        Assert.Equal(12, inv.CountOf("potion"));
        Assert.Equal(3, inv.Add(Potion, 3));                 // tops up slot 2
        Assert.Equal(5, inv.Get(2).Count);
    }

    [Fact]
    public void FullInventoryAddsOnlyWhatFitsAndReportsIt()
    {
        var inv = new Inventory();
        for (int i = 0; i < 10; i++) Assert.Equal(1, inv.Add(Rock));
        Assert.True(inv.IsFull);
        Assert.False(inv.CanAdd(Sword));
        Assert.Equal(0, inv.Add(Sword));
        Assert.Equal(0, inv.CountOf("sword"));

        var inv2 = new Inventory(2);
        Assert.Equal(9, inv2.Add(Potion, 9));                 // 2 slots x 5 = 10 room
        Assert.Equal(1, inv2.Room(Potion));
    }

    [Fact]
    public void PartialAddReturnsTheNumberThatFit()
    {
        var inv = new Inventory(2);
        Assert.Equal(10, inv.Add(Potion, 14));
        Assert.True(inv.IsFull);
        Assert.Equal(0, inv.Room(Potion));
    }

    [Fact]
    public void RemoveTakesFromASlotAndEmptiesIt()
    {
        var inv = new Inventory();
        inv.Add(Potion, 3);
        Assert.Equal(2, inv.Remove(0, 2));
        Assert.Equal(1, inv.Get(0).Count);
        Assert.Equal(5, inv.Remove(0, 5) + 4);                 // only 1 left to take
        Assert.Null(inv.Get(0));
        Assert.Equal(0, inv.Remove(0));                        // already empty
    }

    [Fact]
    public void RemoveItemByIdSpansStacks()
    {
        var inv = new Inventory();
        inv.Add(Potion, 8);
        Assert.Equal(6, inv.RemoveItem("potion", 6));
        Assert.Equal(2, inv.CountOf("potion"));
        Assert.Equal(2, inv.RemoveItem("potion", 10));
        Assert.False(inv.Contains("potion"));
    }

    [Fact]
    public void SwapMovesStacksAround()
    {
        var inv = new Inventory();
        inv.Add(Sword);
        inv.Swap(0, 4);
        Assert.Null(inv.Get(0));
        Assert.Equal("Sword", inv.Get(4).Item.Name);
        Assert.Equal(4, inv.IndexOf("sword"));
    }

    [Fact]
    public void ChangedEventAndVersionTrackEveryChange()
    {
        var inv = new Inventory();
        int events = 0;
        inv.Changed += (_, _) => events++;
        int v = inv.Version;
        inv.Add(Sword);
        inv.Remove(0);
        Assert.Equal(2, events);
        Assert.Equal(v + 2, inv.Version);
        inv.Remove(0);                                         // nothing there, no change
        Assert.Equal(2, events);
    }

    [Fact]
    public void BadIndexesAndItemsAreRejected()
    {
        var inv = new Inventory();
        Assert.Throws<ArgumentOutOfRangeException>(() => inv.Get(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => inv.Get(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => inv.Add(Rock, 0));
        Assert.Throws<ArgumentException>(() => new Item("x", "X", ItemType.Weapon, EquipSlot.MainHand, maxStack: 5));
        Assert.Throws<ArgumentException>(() => new Item("", "X", ItemType.Misc));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStack(Rock, 2));
    }
}
