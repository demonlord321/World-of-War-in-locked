using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

public class EquipmentTests
{
    private static readonly Item Sword = new("sword", "Sword", ItemType.Weapon, EquipSlot.MainHand);
    private static readonly Item Dagger = new("dagger", "Dagger", ItemType.Weapon, EquipSlot.OffHand);
    private static readonly Item Shield = new("shield", "Shield", ItemType.Shield, EquipSlot.OffHand);
    private static readonly Item Helm = new("helm", "Helm", ItemType.Armour, EquipSlot.Head);
    private static readonly Item Boots = new("boots", "Boots", ItemType.Armour, EquipSlot.Feet);
    private static readonly Item Ring = new("ring", "Ring", ItemType.Accessory, EquipSlot.Accessory);
    private static readonly Item Ring2 = new("ring2", "Ring 2", ItemType.Accessory, EquipSlot.Accessory);
    private static readonly Item Rock = new("rock", "Rock", ItemType.Misc, maxStack: 1);   // one per slot so tests can fill the bag

    private static Equipment NewEquipment(out Inventory inv)
    {
        inv = new Inventory();
        return new Equipment(inv);
    }

    [Fact]
    public void SlotOrderMatchesTheMenuAndHasTenSlots()
    {
        Assert.Equal(10, EquipmentSlots.Count);
        Assert.Equal(new[] { "Head", "Shoulder", "Chest", "Legs", "Feet", "Main Hand", "Off Hand", "Accessory 1", "Accessory 2", "Accessory 3" },
            EquipmentSlots.All.Select(EquipmentSlots.Label));
        for (int i = 0; i < 10; i++) Assert.Equal(i, (int)EquipmentSlots.All[i]);
    }

    [Fact]
    public void EquipMovesTheItemOutOfTheInventory()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Helm);

        Assert.Equal(EquipResult.Success, eq.Equip(0));

        Assert.Same(Helm, eq.Get(EquipmentSlot.Head));
        Assert.Null(inv.Get(0));
    }

    [Fact]
    public void EquippingIntoAnOccupiedSlotSwapsWithTheInventory()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Rock);
        inv.Add(Sword);
        Item other = new("sword2", "Sword 2", ItemType.Weapon, EquipSlot.MainHand);
        inv.Add(other);
        eq.Equip(1);

        Assert.Equal(EquipResult.Success, eq.Equip(2));       // swap

        Assert.Same(other, eq.Get(EquipmentSlot.MainHand));
        Assert.Same(Sword, inv.Get(2).Item);                  // old sword takes the freed slot
    }

    [Fact]
    public void SlotRulesRejectIllegalItems()
    {
        Assert.True(Equipment.CanEquip(Helm, EquipmentSlot.Head));
        Assert.False(Equipment.CanEquip(Helm, EquipmentSlot.Feet));
        Assert.False(Equipment.CanEquip(Boots, EquipmentSlot.Head));
        Assert.False(Equipment.CanEquip(Sword, EquipmentSlot.Chest));
        Assert.True(Equipment.CanEquip(Sword, EquipmentSlot.MainHand));
        Assert.False(Equipment.CanEquip(Shield, EquipmentSlot.MainHand));
        Assert.True(Equipment.CanEquip(Shield, EquipmentSlot.OffHand));
        Assert.True(Equipment.CanEquip(Dagger, EquipmentSlot.OffHand));
        Assert.True(Equipment.CanEquip(Sword, EquipmentSlot.OffHand));      // any weapon in the off hand
        Assert.False(Equipment.CanEquip(Dagger, EquipmentSlot.MainHand));
        Assert.False(Equipment.CanEquip(Ring, EquipmentSlot.Head));
        Assert.False(Equipment.CanEquip(Helm, EquipmentSlot.Accessory1));
        Assert.False(Equipment.CanEquip(Rock, EquipmentSlot.MainHand));
        Assert.False(Equipment.CanEquip(null, EquipmentSlot.Head));
        foreach (var slot in new[] { EquipmentSlot.Accessory1, EquipmentSlot.Accessory2, EquipmentSlot.Accessory3 })
            Assert.True(Equipment.CanEquip(Ring, slot));
    }

    [Fact]
    public void IllegalEquipChangesNothing()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Helm);
        inv.Add(Rock);

        Assert.Equal(EquipResult.WrongSlot, eq.Equip(0, EquipmentSlot.Feet));
        Assert.Equal(EquipResult.NotEquippable, eq.Equip(1));
        Assert.Equal(EquipResult.NoItem, eq.Equip(5));
        Assert.Equal(EquipResult.NoItem, eq.Equip(99));
        Assert.Equal(EquipResult.NoItem, eq.Equip(-1));

        Assert.Same(Helm, inv.Get(0).Item);
        Assert.Null(eq.Get(EquipmentSlot.Feet));
        Assert.Equal(0, eq.Version);
    }

    [Fact]
    public void ExplicitTargetPutsAWeaponInTheOffHand()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Sword);
        Assert.Equal(EquipResult.Success, eq.Equip(0, EquipmentSlot.OffHand));
        Assert.Same(Sword, eq.Get(EquipmentSlot.OffHand));
        Assert.Null(eq.Get(EquipmentSlot.MainHand));
    }

    [Fact]
    public void AccessoriesFillTheThreeSlotsInOrder()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Ring);
        inv.Add(Ring2);
        eq.Equip(0);
        eq.Equip(1);
        Assert.Same(Ring, eq.Get(EquipmentSlot.Accessory1));
        Assert.Same(Ring2, eq.Get(EquipmentSlot.Accessory2));
        Assert.Null(eq.Get(EquipmentSlot.Accessory3));
    }

    [Fact]
    public void UnequipReturnsTheItemToTheInventory()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Sword);
        eq.Equip(0);

        Assert.Equal(UnequipResult.Success, eq.Unequip(EquipmentSlot.MainHand));

        Assert.Null(eq.Get(EquipmentSlot.MainHand));
        Assert.Same(Sword, inv.Get(0).Item);
    }

    [Fact]
    public void UnequipFailsWhenTheInventoryIsFullAndKeepsTheItemWorn()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Sword);
        eq.Equip(0);
        for (int i = 0; i < 10; i++) inv.Add(Rock);
        Assert.True(inv.IsFull);

        Assert.Equal(UnequipResult.InventoryFull, eq.Unequip(EquipmentSlot.MainHand));

        Assert.Same(Sword, eq.Get(EquipmentSlot.MainHand));
        Assert.Equal(0, inv.CountOf("sword"));
    }

    [Fact]
    public void UnequipOnAnEmptySlotSaysSo()
    {
        var eq = NewEquipment(out _);
        Assert.Equal(UnequipResult.NothingEquipped, eq.Unequip(EquipmentSlot.Head));
    }

    [Fact]
    public void SwapWorksEvenWithAFullInventory()
    {
        var eq = NewEquipment(out var inv);
        inv.Add(Sword);
        eq.Equip(0);
        Item other = new("sword2", "Sword 2", ItemType.Weapon, EquipSlot.MainHand);
        inv.Add(other);
        for (int i = 0; i < 9; i++) inv.Add(Rock);
        Assert.True(inv.IsFull);

        Assert.Equal(EquipResult.Success, eq.Equip(inv.IndexOf("sword2")));

        Assert.Same(other, eq.Get(EquipmentSlot.MainHand));
        Assert.True(inv.Contains("sword"));
    }

    [Fact]
    public void ChangedEventFires()
    {
        var eq = NewEquipment(out var inv);
        int events = 0;
        eq.Changed += (_, _) => events++;
        inv.Add(Sword);
        eq.Equip(0);
        eq.Unequip(EquipmentSlot.MainHand);
        Assert.Equal(2, events);
        Assert.Equal(2, eq.Version);
    }

    [Fact]
    public void AddAndEquipWearsTheItemAndHandlesFullInventories()
    {
        var eq = NewEquipment(out var inv);
        Assert.Equal(EquipResult.Success, eq.AddAndEquip(Sword));
        Assert.Same(Sword, eq.Get(EquipmentSlot.MainHand));
        Assert.Equal(0, inv.CountOf("sword"));

        Item other = new("sword2", "Sword 2", ItemType.Weapon, EquipSlot.MainHand);
        for (int i = 0; i < 10; i++) inv.Add(Rock);
        Assert.Equal(EquipResult.InventoryFull, eq.AddAndEquip(other));   // hand busy, bag full

        var eq2 = NewEquipment(out var inv2);
        for (int i = 0; i < 10; i++) inv2.Add(Rock);
        Assert.Equal(EquipResult.Success, eq2.AddAndEquip(Sword));          // hand free: worn directly
        Assert.Equal(EquipResult.NotEquippable, eq2.AddAndEquip(Rock));
    }
}
