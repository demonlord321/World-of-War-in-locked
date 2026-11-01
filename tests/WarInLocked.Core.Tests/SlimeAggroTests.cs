using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

/// <summary>Passive slimes that fight back once hit, and the 15 s combat window.</summary>
public class SlimeAggroTests
{
    private static Character Hero()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static Enemy Slime(int level = 1, int x = 6, int y = 5) { var e = Enemy.NewSlime(level); e.TileX = x; e.TileY = y; return e; }

    [Fact]
    public void CombatWindowIsFifteenSeconds()
    {
        Assert.Equal(15.0, CombatController.CombatDurationSeconds);
        var c = Hero();
        c.TakeDamage(1);
        c.Combat.Update(14.9);
        Assert.True(c.InCombat);
        c.Combat.Update(0.2);
        Assert.False(c.InCombat);
    }

    [Fact]
    public void RationsRestoreHealthAndEnergyOnceOutOfCombat()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.AdventurersRations);
        c.TakeDamage(50);
        c.SpendEnergy(20);
        c.Combat.Update(14);
        Assert.Equal("Can't use in combat", c.CanUse(ItemLibrary.AdventurersRationsId));
        c.Combat.Update(1.1);
        Assert.True(c.UseItem(ItemLibrary.AdventurersRationsId).Success);
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(c.MaxEnergy, c.Energy);
    }

    [Fact]
    public void SlimesArePassiveUntilDamaged()
    {
        var c = Hero();
        var s = Slime();
        Assert.False(s.IsAggro);
        int lost = c.Combat.UpdateEnemies(new[] { s }, 30, 5, 5);
        Assert.Equal(0, lost);
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.False(c.InCombat);
    }

    [Fact]
    public void DamagingASlimeAggrosItAndItHitsImmediatelyThenEverySixSeconds()
    {
        var c = Hero();
        var s = Slime(2);
        var list = new[] { s };
        c.Combat.BasicAttack(5, 5, list);
        Assert.True(s.IsAggro);
        Assert.Equal(6.0, Enemy.AttackIntervalSeconds);
        Assert.Equal(0, s.AttackCooldownRemaining);                       // ready: the first hit is immediate

        Assert.Equal(12, c.Combat.UpdateEnemies(list, 0.0, 5, 5));
        Assert.Equal(c.MaxHealth - 12, c.Health);
        Assert.Equal(Enemy.AttackIntervalSeconds, s.AttackCooldownRemaining);
        Assert.Equal(0, c.Combat.UpdateEnemies(list, Enemy.AttackIntervalSeconds - 0.1, 5, 5));
        Assert.Equal(12, c.Combat.UpdateEnemies(list, 0.2, 5, 5));
        Assert.Equal(c.MaxHealth - 24, c.Health);
    }

    [Fact]
    public void SlimeDamageIsItsLevelAttack()
    {
        foreach (var (level, atk) in new[] { (1, 10), (2, 12), (3, 15) })
        {
            var c = Hero();
            var s = Slime(level);
            s.Provoke();
            Assert.Equal(atk, c.Combat.UpdateEnemies(new[] { s }, Enemy.AttackIntervalSeconds, 5, 5));
        }
    }

    [Fact]
    public void SlimeHitsStartTheCombatWindowAndRaiseTheDamagedEvent()
    {
        var c = Hero();
        var seen = new List<int>();
        c.Damaged += n => seen.Add(n);
        var s = Slime(3);
        s.Provoke();
        c.Combat.UpdateEnemies(new[] { s }, Enemy.AttackIntervalSeconds, 5, 5);
        Assert.Equal(new[] { 15 }, seen);
        Assert.True(c.InCombat);
        Assert.Equal(15.0, c.CombatCooldownRemaining, 3);
        Assert.True(s.AttackFlash > 0);
    }

    [Fact]
    public void AnAggroSlimeOutOfReachWaitsThenHitsWhenCloseAgain()
    {
        var c = Hero();
        var s = Slime(1, 9, 9);
        s.Provoke();
        Assert.Equal(0, c.Combat.UpdateEnemies(new[] { s }, 10, 5, 5, (x, y) => false));   // cannot chase (walled in): waits
        Assert.Equal(10, c.Combat.UpdateEnemies(new[] { s }, 0.1, 8, 8));
    }

    [Fact]
    public void ADeadSlimeDoesNotAttackAndAggroEndsWithItsDeath()
    {
        var c = Hero();
        var s = Slime();
        s.TakeDamage(1);
        Assert.True(s.IsAggro);
        s.TakeDamage(1000);
        Assert.True(s.IsDead);
        Assert.False(s.IsAggro);
        Assert.Equal(0, c.Combat.UpdateEnemies(new[] { s }, 10, 5, 5));
    }

    [Fact]
    public void ARespawnedSlimeIsPassiveAgain()
    {
        var old = Slime();
        old.TakeDamage(1);
        Assert.True(old.IsAggro);
        var fresh = Enemy.NewSlime(1);
        Assert.False(fresh.IsAggro);
        Assert.Equal(0, fresh.AttackCooldownRemaining);
    }

    [Fact]
    public void PlayerHealthClampsAtZeroAndDiedFiresOnce()
    {
        var c = Hero();
        int died = 0;
        c.Died += () => died++;
        var s = Slime(3);
        s.Provoke();
        var list = new[] { s };
        int hits = 0;
        while (!c.IsDead && hits++ < 100) c.Combat.UpdateEnemies(list, Enemy.AttackIntervalSeconds, 5, 5);
        Assert.True(c.IsDead);
        Assert.Equal(0, c.Health);
        Assert.Equal(1, died);
        // Further hits do nothing: the dead player is not attacked and takes nothing.
        Assert.Equal(0, c.Combat.UpdateEnemies(list, 30, 5, 5));
        Assert.Equal(0, c.TakeDamage(10));
        Assert.Equal(1, died);
    }
}
