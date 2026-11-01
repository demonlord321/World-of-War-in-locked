using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

/// <summary>#26 Adventurer's rations and the in-combat window.</summary>
public class RationsTests
{
    private static Character Hero(int level = 1)
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        while (c.Level < level) c.GainXp(c.XpForNextLevel);
        Assert.Equal(level, c.Level);
        c.Inventory.Add(ItemLibrary.AdventurersRations, 3);
        return c;
    }

    private static void Hurt(Character c)
    {
        c.TakeDamage(c.MaxHealth / 2);
        c.Combat.Update(CombatController.CombatDurationSeconds + 0.1);   // out of combat again
        c.SpendEnergy(c.MaxEnergy / 2);
        Assert.False(c.InCombat);
    }

    [Fact]
    public void RationsHaveDyllansTextAndAreUsable()
    {
        var r = ItemLibrary.AdventurersRations;
        Assert.Equal("When outside of combat, recover completely. Up till level 10. After that the rations are only half as effective.", r.Description);
        Assert.True(r.IsUsable);
        Assert.Equal(new[] { "Cooldown after use 5s" }, r.EffectLines);
    }

    [Fact]
    public void RationsRestoreHealthAndEnergyCompletelyUpToLevelTen()
    {
        foreach (int level in new[] { 1, 10 })
        {
            var c = Hero(level);
            Hurt(c);
            var r = c.UseItem(ItemLibrary.AdventurersRationsId);
            Assert.True(r.Success);
            Assert.Equal(c.MaxHealth, c.Health);
            Assert.Equal(c.MaxEnergy, c.Energy);
            Assert.True(r.Healed > 0);
            Assert.True(r.EnergyRestored > 0);
            Assert.Equal(2, c.Inventory.CountOf(ItemLibrary.AdventurersRationsId));
            Assert.Equal(5, c.ItemCooldownRemaining);
        }
    }

    [Fact]
    public void AboveLevelTenRationsRestoreHalfOfTheMaxCapped()
    {
        var c = Hero(11);
        c.TakeDamage(c.MaxHealth - 1);
        c.Combat.Update(6);
        c.SpendEnergy(c.MaxEnergy);
        int hp = c.Health;
        var r = c.UseItem(ItemLibrary.AdventurersRationsId);
        Assert.True(r.Success);
        Assert.Equal(hp + (int)System.Math.Round(c.MaxHealth * 0.5, System.MidpointRounding.AwayFromZero), c.Health);
        Assert.Equal((int)System.Math.Floor(c.MaxEnergy * 0.5), c.Energy);

        // Only slightly hurt: the half is capped at the max.
        var d = Hero(11);
        d.TakeDamage(3);
        d.Combat.Update(6);
        d.UseItem(ItemLibrary.AdventurersRationsId);
        Assert.Equal(d.MaxHealth, d.Health);
        Assert.Equal(d.MaxEnergy, d.Energy);
    }

    [Fact]
    public void RationsAreRefusedInCombatAndNothingIsSpent()
    {
        var c = Hero();
        c.TakeDamage(10);                       // took damage: in combat
        Assert.True(c.InCombat);
        Assert.Equal("Can't use in combat", c.CanUse(ItemLibrary.AdventurersRationsId));
        var r = c.UseItem(ItemLibrary.AdventurersRationsId);
        Assert.False(r.Success);
        Assert.Equal("Can't use in combat", r.Reason);
        Assert.Equal(3, c.Inventory.CountOf(ItemLibrary.AdventurersRationsId));
        Assert.Equal(0, c.ItemCooldownRemaining);
        c.Combat.Update(4.9);
        Assert.Equal("Can't use in combat", c.CanUse(ItemLibrary.AdventurersRationsId));
        c.Combat.Update(0.2);
        Assert.False(c.InCombat);
        Assert.Null(c.CanUse(ItemLibrary.AdventurersRationsId));
    }

    [Fact]
    public void DealingDamageStartsTheCombatWindowToo()
    {
        var c = Hero();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        Assert.False(c.InCombat);
        var slime = new Enemy(Enemy.SlimeType, 1000) { TileX = 6, TileY = 5 };
        c.Combat.BasicAttack(5, 5, new List<Enemy> { slime });
        Assert.True(c.InCombat);
        Assert.Equal(5, c.CombatCooldownRemaining, 3);
        c.Combat.Update(2);
        Assert.Equal(3, c.CombatCooldownRemaining, 3);
    }

    [Fact]
    public void RationsAreRefusedWhenHealthAndEnergyAreFull()
    {
        var c = Hero();
        Assert.Equal("Already fully recovered", c.CanUse(ItemLibrary.AdventurersRationsId));
        Assert.False(c.UseItem(ItemLibrary.AdventurersRationsId).Success);
        Assert.Equal(3, c.Inventory.CountOf(ItemLibrary.AdventurersRationsId));

        // Only energy missing is enough to use them.
        c.SpendEnergy(10);
        Assert.Null(c.CanUse(ItemLibrary.AdventurersRationsId));
    }

    [Fact]
    public void RationsShareTheItemCooldownWithPotions()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion);
        Hurt(c);
        Assert.True(c.UseItem(ItemLibrary.LesserHealingPotionId).Success);
        Assert.Equal("On cooldown", c.CanUse(ItemLibrary.AdventurersRationsId));
        c.Combat.Update(5.1);
        Assert.True(c.UseItem(ItemLibrary.AdventurersRationsId).Success);
    }

    [Fact]
    public void RationsWorkFromTheHotbarWithoutATarget()
    {
        var c = Hero();
        Hurt(c);
        c.Combat.AssignItemToHotbar(0, ItemLibrary.AdventurersRationsId);
        var r = c.Combat.UseHotbar(0, 0, 0, new List<Enemy>());
        Assert.Equal(CombatOutcome.ItemUsed, r.Outcome);
        Assert.Equal(c.MaxHealth, c.Health);
    }
}
