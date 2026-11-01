using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

/// <summary>#36: slimes give XP (5 x level) and drop exactly one Slime Jelly, no copper.</summary>
public class SlimeRewardsTests
{
    private static Character Hero()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static Enemy SlimeAt(int level) { var e = Enemy.NewSlime(level); e.TileX = 6; e.TileY = 5; return e; }

    /// <summary>Hits the slime until it dies and returns the killing result.</summary>
    private static CombatResult Kill(Character c, Enemy s)
    {
        for (int i = 0; i < 200; i++)
        {
            c.Combat.Update(0.6);
            var r = c.Combat.BasicAttack(5, 5, new[] { s });
            if (r.Outcome == CombatOutcome.Killed) return r;
        }
        throw new System.InvalidOperationException("slime did not die");
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 15)]
    [InlineData(4, 20)]    // placeholder 5 x level
    [InlineData(5, 25)]
    [InlineData(11, 55)]   // a night level 1 slime
    public void SlimesGiveFiveXpPerLevel(int level, int xp)
    {
        Assert.Equal(xp, SlimeAt(level).XpReward);
        var c = Hero();
        var r = Kill(c, SlimeAt(level));
        Assert.Equal(xp, r.XpGained);
        Assert.Equal(xp, c.Xp);
    }

    [Fact]
    public void EveryKillDropsExactlyOneSlimeJellyWhateverTheLevelAndTheyStack()
    {
        var c = Hero();
        foreach (int level in new[] { 1, 2, 3, 4, 5 })
        {
            var r = Kill(c, SlimeAt(level));
            Assert.Same(ItemLibrary.SlimeJelly, r.DroppedItem);
            Assert.False(r.DropLost);
        }
        Assert.Equal(5, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
    }

    [Fact]
    public void ADamagedSlimeThatSurvivesGivesNothingAndNoCopperEverDrops()
    {
        var c = Hero();
        var s = SlimeAt(1);
        var hit = c.Combat.BasicAttack(5, 5, new[] { s });
        Assert.Equal(CombatOutcome.Hit, hit.Outcome);
        Assert.Equal(0, hit.XpGained);
        Assert.Null(hit.DroppedItem);
        Assert.Equal(0, c.Xp);
        Assert.Equal(0, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        var kill = Kill(c, s);
        Assert.Equal(0, c.Copper);
        Assert.Equal(5, kill.XpGained);
    }

    [Fact]
    public void SlimeJellyHasNoUseExceptSellingAndIsRegistered()
    {
        var j = ItemLibrary.SlimeJelly;
        Assert.Equal("Slime Jelly", j.Name);
        Assert.Equal(ItemType.Misc, j.Type);
        Assert.False(j.IsUsable);
        Assert.False(j.IsEquippable);
        Assert.True(j.IsSellable);
        Assert.Equal(3, j.SellPriceCopper);                 // placeholder
        Assert.True(j.MaxStack > 1);
        Assert.Same(j, ItemLibrary.Get(ItemLibrary.SlimeJellyId));
        Assert.False(ItemLibrary.PracticeSword.IsSellable);  // nothing else has a sell price yet
    }

    [Fact]
    public void AFullInventoryLosesTheDropButStillGivesTheXp()
    {
        var c = Hero();
        var junk = new Item("junk", "Junk", ItemType.Misc);
        while (!c.Inventory.IsFull) c.Inventory.Add(junk, 1);
        var r = Kill(c, SlimeAt(2));
        Assert.True(r.DropLost);
        Assert.Same(ItemLibrary.SlimeJelly, r.DroppedItem);
        Assert.Equal(0, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
        Assert.Equal(10, r.XpGained);
        Assert.Equal(10, c.Xp);
        Assert.False(string.IsNullOrEmpty(CombatController.DropLostText));
    }

    [Fact]
    public void AFullInventoryStillStacksJellyOntoAnExistingStack()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.SlimeJelly, 1);
        var junk = new Item("junk", "Junk", ItemType.Misc);
        while (!c.Inventory.IsFull) c.Inventory.Add(junk, 1);
        var r = Kill(c, SlimeAt(1));
        Assert.False(r.DropLost);
        Assert.Equal(2, c.Inventory.CountOf(ItemLibrary.SlimeJellyId));
    }

    [Fact]
    public void EnoughKillXpLevelsTheCharacterUp()
    {
        var c = Hero();
        int levelsSeen = 0;
        c.LeveledUp += _ => levelsSeen++;
        int total = 0;
        while (c.Level == 1) total += Kill(c, SlimeAt(3)).LevelsGained;
        Assert.Equal(1, total);
        Assert.Equal(1, levelsSeen);
        Assert.Equal(2, c.Level);
    }

    [Fact]
    public void OtherEnemyTypesGiveNothingYet()
    {
        var e = new Enemy("dummy", 10);
        Assert.Equal(0, e.XpReward);
        Assert.Null(e.DropItem);
    }

    [Fact]
    public void JellySurvivesInTheInventoryLikeAnyItem()
    {
        var c = Hero();
        Kill(c, SlimeAt(1));
        Assert.Equal(1, c.Inventory.CountOf("slime-jelly"));
        Assert.Equal("A wobbling blob of jelly. Good for nothing but selling.", ItemLibrary.SlimeJelly.Description.Trim());
    }
}
