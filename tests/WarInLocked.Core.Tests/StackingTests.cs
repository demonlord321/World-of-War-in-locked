using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;
using WarInLocked.Core.Saving;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>Dyllan: items of the same thing stack, except equipment (one per slot). The cap is 99 (confirmed).</summary>
public class StackingTests
{
    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    [Fact]
    public void EveryNonEquipmentItemStacksToNinetyNineByDefault()
    {
        Assert.Equal(99, Item.DefaultStackSize);
        foreach (var i in ItemLibrary.All)
        {
            if (i.IsEquippable) { Assert.Equal(1, i.MaxStack); Assert.False(i.IsStackable); }
            else { Assert.Equal(99, i.MaxStack); Assert.True(i.IsStackable); }
        }
        var misc = new Item("thing", "Thing", ItemType.Misc);       // no maxStack given: the rule decides
        Assert.Equal(99, misc.MaxStack);
        var quest = new Item("letter", "Letter", ItemType.Misc);
        Assert.Equal(99, quest.MaxStack);
        var sword = new Item("sword", "Sword", ItemType.Weapon, EquipSlot.MainHand);
        Assert.Equal(1, sword.MaxStack);
    }

    [Fact]
    public void EquipmentCanNeverBeGivenAStackSize()
    {
        Assert.Throws<ArgumentException>(() => new Item("x", "X", ItemType.Weapon, EquipSlot.MainHand, maxStack: 5));
    }

    [Fact]
    public void RationsPotionsAndJellyStackInOneSlot()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.AdventurersRations, 7);
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 3);
        c.Inventory.Add(ItemLibrary.SlimeJelly, 1);
        c.Inventory.Add(ItemLibrary.AdventurersRations, 2);
        c.Inventory.Add(ItemLibrary.SlimeJelly, 4);
        Assert.Equal(3, c.Inventory.Capacity - c.Inventory.FreeSlots);
        Assert.Equal(9, c.Inventory.CountOf(ItemLibrary.AdventurersRationsId));
        Assert.Equal(9, c.Inventory.Get(0).Count);
        Assert.Equal(3, c.Inventory.Get(1).Count);
        Assert.Equal(5, c.Inventory.Get(2).Count);
    }

    [Fact]
    public void AFullStackOverflowsIntoTheNextSlot()
    {
        var inv = new Inventory();
        Assert.Equal(250, inv.Add(ItemLibrary.SlimeJelly, 250));
        Assert.Equal(99, inv.Get(0).Count);
        Assert.Equal(99, inv.Get(1).Count);
        Assert.Equal(52, inv.Get(2).Count);
        Assert.Equal(1, inv.Add(ItemLibrary.SlimeJelly));         // tops up the partial stack, no new slot
        Assert.Equal(53, inv.Get(2).Count);
        Assert.Null(inv.Get(3));
    }

    [Fact]
    public void EquipmentNeverStacksOneItemPerSlot()
    {
        var inv = new Inventory();
        Assert.Equal(1, inv.Add(ItemLibrary.PracticeSword));
        Assert.Equal(1, inv.Add(ItemLibrary.PracticeSword));
        Assert.Equal(1, inv.Add(ItemLibrary.PracticeSword));
        Assert.Equal(1, inv.Get(0).Count);
        Assert.Equal(1, inv.Get(1).Count);
        Assert.Equal(1, inv.Get(2).Count);
        Assert.Equal(3, inv.CountOf(ItemLibrary.PracticeSwordId));
        Assert.Equal(7, inv.FreeSlots);
        Assert.Equal(1, inv.Add(ItemLibrary.PracticeStaff));
        Assert.Equal(1, inv.Get(3).Count);
    }

    [Fact]
    public void AFullInventoryStillTakesItemsThatStackOntoExistingStacks()
    {
        var inv = new Inventory(2);
        inv.Add(ItemLibrary.PracticeSword);
        inv.Add(ItemLibrary.SlimeJelly, 98);
        Assert.True(inv.IsFull);
        Assert.True(inv.CanAdd(ItemLibrary.SlimeJelly, 1));
        Assert.False(inv.CanAdd(ItemLibrary.SlimeJelly, 2));
        Assert.False(inv.CanAdd(ItemLibrary.PracticeSword));
        Assert.False(inv.CanAdd(ItemLibrary.AdventurersRations));
        Assert.Equal(1, inv.Add(ItemLibrary.SlimeJelly, 5));       // only one fits
        Assert.Equal(99, inv.CountOf(ItemLibrary.SlimeJellyId));
    }

    [Fact]
    public void UsingAnItemTakesOneFromTheStack()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 5);
        c.TakeDamage(100);
        var r = c.UseItem(ItemLibrary.LesserHealingPotionId);
        Assert.True(r.Success);
        Assert.Equal(4, c.Inventory.Get(0).Count);
        Assert.Equal(4, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
    }

    [Fact]
    public void UsingTheLastOneEmptiesTheSlot()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 1);
        c.TakeDamage(100);
        Assert.True(c.UseItem(ItemLibrary.LesserHealingPotionId).Success);
        Assert.Null(c.Inventory.Get(0));
        Assert.Equal(10, c.Inventory.FreeSlots);
    }

    [Fact]
    public void RemovingAcrossStacksTakesFromTheLastFirst()
    {
        var inv = new Inventory();
        inv.Add(ItemLibrary.SlimeJelly, 104);                      // 99 + 5
        Assert.Equal(8, inv.RemoveItem(ItemLibrary.SlimeJellyId, 8));
        Assert.Null(inv.Get(1));                                   // the 5 are gone, 3 more came from the first
        Assert.Equal(96, inv.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Equal(96, inv.Get(0).Count);
        Assert.Equal(3, inv.Remove(0, 3));
        Assert.Equal(93, inv.Get(0).Count);
        Assert.Equal(93, inv.Remove(0, 99));                       // asking for more removes what is there
        Assert.Null(inv.Get(0));
    }

    [Fact]
    public void StackCountsSurviveSaveAndLoad()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.PracticeSword);
        c.Inventory.Add(ItemLibrary.PracticeSword);
        c.Inventory.Add(ItemLibrary.SlimeJelly, 150);
        c.Inventory.Add(ItemLibrary.AdventurersRations, 12);
        var back = SaveGame.FromJson(SaveGame.Capture(c, MapIds.Forest, 5, 5).ToJson()).ToCharacter();
        Assert.Equal(150, back.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Equal(12, back.Inventory.CountOf(ItemLibrary.AdventurersRationsId));
        Assert.Equal(2, back.Inventory.CountOf(ItemLibrary.PracticeSwordId));
        for (int i = 0; i < c.Inventory.Capacity; i++)
        {
            Assert.Equal(c.Inventory.Get(i)?.Item.Id, back.Inventory.Get(i)?.Item.Id);
            Assert.Equal(c.Inventory.Get(i)?.Count, back.Inventory.Get(i)?.Count);
        }
    }

    [Fact]
    public void ASavedStackBiggerThanTheCapSpillsOverInsteadOfVanishing()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 10);
        var s = SaveGame.Capture(c, MapIds.Forest, 5, 5);
        s.Inventory[0].Count = 120;                                // e.g. written when the cap was higher
        var back = s.ToCharacter();
        Assert.Equal(120, back.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Equal(99, back.Inventory.Get(0).Count);
        Assert.Equal(21, back.Inventory.Get(1).Count);
    }

    [Fact]
    public void EquippingOneOfTwoSwordsLeavesTheOtherInItsSlot()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.PracticeSword);
        c.Inventory.Add(ItemLibrary.PracticeSword);
        Assert.Equal(EquipResult.Success, c.Equipment.Equip(0));
        Assert.Null(c.Inventory.Get(0));
        Assert.Equal(1, c.Inventory.Get(1).Count);
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.PracticeSwordId));
    }

    [Fact]
    public void TheStackLimitIsPartOfTheItemDescriptionForTheUi()
    {
        var s = new ItemStack(ItemLibrary.SlimeJelly, 99);
        Assert.Equal("Slime Jelly x99", s.ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStack(ItemLibrary.SlimeJelly, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStack(ItemLibrary.PracticeSword, 2));
    }
}
