using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

/// <summary>#36/#62: kills leave items on the ground, the player picks them up or not, and can drop items.</summary>
public class GroundItemsTests
{
    private static Character Hero()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.EnterMap("forest");
        return c;
    }

    private static void FillWithJunk(Character c)
    {
        var junk = new Item("junk", "Junk", ItemType.Misc, maxStack: 1);
        while (!c.Inventory.IsFull) c.Inventory.Add(junk, 1);
    }

    [Fact]
    public void CapacityTextIsExactlyDyllans()
    {
        Assert.Equal("Inventory currently at capacity.", GroundItems.CapacityText);
    }

    [Fact]
    public void PickingUpTakesTheItemOffTheGround()
    {
        var c = Hero();
        var g = c.Combat.Ground;
        g.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        var r = c.PickUp(5, 5);
        Assert.Equal(PickUpOutcome.PickedUp, r.Outcome);
        Assert.True(r.Success);
        Assert.Equal(1, r.Taken);
        Assert.Null(r.Message);
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Empty(g.Items);
    }

    [Fact]
    public void NothingNearbyPicksNothingUp()
    {
        var c = Hero();
        c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 9, 9);
        Assert.Equal(PickUpOutcome.NothingThere, c.PickUp(5, 5).Outcome);
        Assert.Equal(1, c.Combat.Ground.Count);
    }

    [Fact]
    public void ReachIsTheOwnTileAndTheEightNeighboursNearestFirst()
    {
        var c = Hero();
        var g = c.Combat.Ground;
        var diag = g.Add(ItemLibrary.SlimeJelly, 1, 6, 6);
        var straight = g.Add(ItemLibrary.AdventurersRations, 1, 6, 5);
        var own = g.Add(ItemLibrary.LesserHealingPotion, 1, 5, 5);
        Assert.Same(own, g.FindNear(5, 5));
        g.TryPickUp(c, 5, 5);
        Assert.Same(straight, g.FindNear(5, 5));
        g.TryPickUp(c, 5, 5);
        Assert.Same(diag, g.FindNear(5, 5));
        Assert.Null(g.FindNear(4, 3));
    }

    [Fact]
    public void FullInventoryLeavesTheItemAndSaysSo()
    {
        var c = Hero();
        FillWithJunk(c);
        var gi = c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        var r = c.PickUp(5, 5);
        Assert.Equal(PickUpOutcome.NoRoom, r.Outcome);
        Assert.False(r.Success);
        Assert.Equal("Inventory currently at capacity.", r.Message);
        Assert.Equal(0, r.Taken);
        Assert.Equal(1, r.Remaining);
        Assert.Equal(1, gi.Count);
        Assert.Contains(gi, c.Combat.Ground.Items);
        Assert.Equal(0, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
    }

    [Fact]
    public void AFullInventoryStillTakesAnItemThatStacksOntoItsStack()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 1);
        FillWithJunk(c);
        c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        Assert.Equal(PickUpOutcome.PickedUp, c.PickUp(5, 5).Outcome);
        Assert.Equal(2, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
    }

    [Fact]
    public void ABigStackThatOnlyPartlyFitsTakesWhatFitsAndLeavesTheRest()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 90);
        FillWithJunk(c);                                            // 1 jelly slot with 9 room, nothing else free
        var gi = c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 20, 5, 5);
        var r = c.PickUp(5, 5);
        Assert.Equal(PickUpOutcome.Partial, r.Outcome);
        Assert.True(r.Success);
        Assert.Equal(9, r.Taken);
        Assert.Equal(11, r.Remaining);
        Assert.Equal("Inventory currently at capacity.", r.Message);
        Assert.Equal(99, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Equal(11, gi.Count);
        Assert.Single(c.Combat.Ground.Items);
    }

    [Fact]
    public void AFallenPlayerCannotPickUp()
    {
        var c = Hero();
        c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        c.TakeDamage(10000);
        Assert.True(c.IsDead);
        Assert.Equal(PickUpOutcome.Fallen, c.PickUp(5, 5).Outcome);
        Assert.Equal(1, c.Combat.Ground.Count);
    }

    [Fact]
    public void TheSameItemOnOneTileMergesUpToTheStackCapThenSplits()
    {
        var g = new GroundItems();
        g.Add(ItemLibrary.SlimeJelly, 60, 1, 1);
        g.Add(ItemLibrary.SlimeJelly, 60, 1, 1);
        var here = g.At(1, 1);
        Assert.Equal(2, here.Count);
        Assert.Equal(99, here[0].Count);
        Assert.Equal(21, here[1].Count);
        g.Add(ItemLibrary.SlimeJelly, 1, 2, 2);
        Assert.Equal(3, g.Count);
    }

    [Fact]
    public void GroundItemsRaiseChangedAndBumpVersion()
    {
        var g = new GroundItems();
        int n = 0; g.Changed += () => n++;
        int v = g.Version;
        g.Add(ItemLibrary.SlimeJelly, 1, 1, 1);
        Assert.Equal(1, n);
        Assert.True(g.Version > v);
    }

    // ---- dropping

    [Fact]
    public void DroppingPutsTheStackOnThePlayersTileAndItCanBePickedBackUp()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 12);
        var r = c.DropItem(0, 5, 7, 8);
        Assert.True(r.Dropped);
        Assert.Equal(5, r.Count);
        Assert.Equal(7, c.Inventory.Get(0).Count);
        var onGround = c.Combat.Ground.At(7, 8);
        Assert.Single(onGround);
        Assert.Equal(5, onGround[0].Count);
        Assert.Same(onGround[0], r.Ground);
        Assert.Equal(PickUpOutcome.PickedUp, c.PickUp(7, 8).Outcome);
        Assert.Equal(12, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Empty(c.Combat.Ground.Items);
    }

    [Fact]
    public void DroppingTheWholeStackEmptiesTheSlot()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 3);
        Assert.True(c.DropStack(0, 1, 1).Dropped);
        Assert.Null(c.Inventory.Get(0));
        Assert.Equal(3, c.Combat.Ground.At(1, 1)[0].Count);
    }

    [Fact]
    public void DroppingMoreThanYouHaveDropsTheWholeStack()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.AdventurersRations, 4);
        var r = c.DropItem(0, 50, 1, 1);
        Assert.Equal(4, r.Count);
        Assert.Null(c.Inventory.Get(0));
    }

    [Fact]
    public void DroppingAnEquipmentItemFromTheBagWorks()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.PracticeSword);
        Assert.True(c.DropItem(0, 1, 2, 2).Dropped);
        Assert.Null(c.Inventory.Get(0));
        Assert.Equal(ItemLibrary.PracticeSword, c.Combat.Ground.At(2, 2)[0].Item);
    }

    [Fact]
    public void WornEquipmentIsNotInTheInventorySoItCannotBeDroppedUntilUnequipped()
    {
        var c = Hero();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        Assert.False(c.DropItem(0, 1, 2, 2).Dropped);              // slot 0 is empty: the sword is worn
        Assert.Equal(0, c.Combat.Ground.Count);
        c.Equipment.Unequip(EquipmentSlot.MainHand);
        Assert.True(c.DropItem(0, 1, 2, 2).Dropped);
    }

    [Fact]
    public void QuestItemsCannotBeDropped()
    {
        var c = Hero();
        var letter = new Item("letter", "Letter", ItemType.Misc, questItem: true);
        c.Inventory.Add(letter, 1);
        var r = c.DropItem(0, 1, 2, 2);
        Assert.False(r.Dropped);
        Assert.Equal(Character.CannotDropQuestItemText, r.Message);
        Assert.Equal(1, c.Inventory.Get(0).Count);
        Assert.Equal(0, c.Combat.Ground.Count);
    }

    [Fact]
    public void EmptySlotsAndBadArgumentsDropNothing()
    {
        var c = Hero();
        Assert.False(c.DropItem(0, 1, 1, 1).Dropped);
        Assert.False(c.DropItem(-1, 1, 1, 1).Dropped);
        Assert.False(c.DropItem(99, 1, 1, 1).Dropped);
        c.Inventory.Add(ItemLibrary.SlimeJelly, 2);
        Assert.False(c.DropItem(0, 0, 1, 1).Dropped);
        Assert.Equal(2, c.Inventory.Get(0).Count);
    }

    [Fact]
    public void AFallenPlayerCannotDrop()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 2);
        c.TakeDamage(10000);
        var r = c.DropItem(0, 1, 1, 1);
        Assert.False(r.Dropped);
        Assert.Equal(Character.CannotDropFallenText, r.Message);
    }

    [Fact]
    public void DroppingMakesRoomToPickUpTheNewItem()
    {
        var c = Hero();
        FillWithJunk(c);
        c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        Assert.Equal(PickUpOutcome.NoRoom, c.PickUp(5, 5).Outcome);
        Assert.True(c.DropItem(3, 1, 4, 4).Dropped);                // drop one junk elsewhere
        Assert.Equal(PickUpOutcome.PickedUp, c.PickUp(5, 5).Outcome);
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
    }

    // ---- per map

    [Fact]
    public void EachMapHasItsOwnGroundAndItIsKeptForTheSession()
    {
        var c = Hero();
        c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        var forest = c.Combat.Ground;
        c.EnterMap("path");
        Assert.NotSame(forest, c.Combat.Ground);
        Assert.Empty(c.Combat.Ground.Items);
        c.EnterMap("forest");
        Assert.Same(forest, c.Combat.Ground);
        Assert.Equal(1, c.Combat.Ground.Count);
        Assert.Equal("forest", c.CurrentMapId);
    }

    [Fact]
    public void GroundItemsAreNotSaved()
    {
        var c = Hero();
        c.Combat.Ground.Add(ItemLibrary.SlimeJelly, 1, 5, 5);
        var back = WarInLocked.Core.Saving.SaveGame.FromJson(WarInLocked.Core.Saving.SaveGame.Capture(c, "forest", 5, 5).ToJson()).ToCharacter();
        back.EnterMap("forest");
        Assert.Equal(0, back.Combat.Ground.Count);
    }
}
