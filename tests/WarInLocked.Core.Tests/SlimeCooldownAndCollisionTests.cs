using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

/// <summary>The 6 s slime attack is a cooldown with a reach check on every attack, and tile collision between the player and enemies.</summary>
public class SlimeCooldownAndCollisionTests
{
    private static Character Hero()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static Enemy Slime(int x, int y, int level = 1) { var e = Enemy.NewSlime(level); e.TileX = x; e.TileY = y; return e; }

    [Fact]
    public void ProvokedSlimeIsReadyWithCooldownZero()
    {
        var s = Slime(6, 5);
        s.Provoke();
        Assert.True(s.IsAggro);
        Assert.Equal(0, s.AttackCooldownRemaining);
    }

    [Theory]
    [InlineData(5, 5)]   // own tile
    [InlineData(6, 5)]   // adjacent
    [InlineData(6, 6)]   // diagonal
    public void FirstAttackIsImmediateWhenInReachThenWaitsSixSeconds(int sx, int sy)
    {
        var c = Hero();
        var s = Slime(sx, sy);
        var list = new[] { s };
        s.Provoke();
        Assert.Equal(10, c.Combat.UpdateEnemies(list, 0.016, 5, 5));
        Assert.Equal(c.MaxHealth - 10, c.Health);
        Assert.Equal(0, c.Combat.UpdateEnemies(list, 5.9, 5, 5));
        Assert.Equal(10, c.Combat.UpdateEnemies(list, 0.2, 5, 5));
        Assert.Equal(c.MaxHealth - 20, c.Health);
    }

    [Fact]
    public void OutOfReachSlimeDoesNotAttackAndStaysReady()
    {
        var c = Hero();
        var s = Slime(8, 5);                     // 3 tiles away
        var list = new[] { s };
        s.Provoke();
        for (int i = 0; i < 10; i++) Assert.Equal(0, c.Combat.UpdateEnemies(list, 1.0, 5, 5));
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(0, s.AttackCooldownRemaining);          // still ready, not counting down a stale timer
        Assert.False(c.InCombat);
        Assert.Equal(10, c.Combat.UpdateEnemies(list, 0.016, 7, 5));   // back in reach: hits at once
        Assert.Equal(Enemy.AttackIntervalSeconds, s.AttackCooldownRemaining, 2);
    }

    [Fact]
    public void ReachIsCheckedBeforeEveryAttackNotJustTheFirst()
    {
        var c = Hero();
        var s = Slime(6, 5);
        var list = new[] { s };
        s.Provoke();
        Assert.Equal(10, c.Combat.UpdateEnemies(list, 0.016, 5, 5));
        Assert.Equal(0, c.Combat.UpdateEnemies(list, 6.0, 9, 9));       // cooldown is over but the player stepped away
        Assert.Equal(0, s.AttackCooldownRemaining);
        Assert.Equal(c.MaxHealth - 10, c.Health);
        Assert.Equal(10, c.Combat.UpdateEnemies(list, 0.016, 6, 6));    // back: immediate again
    }

    [Fact]
    public void HittingASlimeThenStandingNextToItGetsHitOnTheNextUpdate()
    {
        var c = Hero();
        var s = Slime(6, 5, 3);
        var list = new[] { s };
        c.Combat.BasicAttack(5, 5, list);
        Assert.Equal(15, c.Combat.UpdateEnemies(list, 0.016, 5, 5));
    }

    [Fact]
    public void DeadSlimeNeverAttacksAndNeverBlocks()
    {
        var c = Hero();
        var s = Slime(5, 5);
        s.Provoke();
        s.TakeDamage(10000);
        Assert.Equal(0, c.Combat.UpdateEnemies(new[] { s }, 10, 5, 5));
        Assert.False(TileOccupancy.IsBlocked(5, 5, new[] { s }));
        Assert.Null(TileOccupancy.EnemyAt(5, 5, new[] { s }));
    }

    [Fact]
    public void PlayerCannotMoveOntoALivingEnemysTile()
    {
        var a = Slime(3, 4);
        var b = Slime(7, 7);
        var list = new[] { a, b };
        Assert.True(TileOccupancy.IsBlocked(3, 4, list));
        Assert.True(TileOccupancy.IsBlocked(7, 7, list));
        Assert.False(TileOccupancy.IsBlocked(3, 5, list));
        Assert.False(TileOccupancy.IsBlocked(0, 0, list));
        Assert.False(TileOccupancy.IsBlocked(3, 4, new Enemy[0]));
        Assert.False(TileOccupancy.IsBlocked(3, 4, null));
        Assert.Same(a, TileOccupancy.EnemyAt(3, 4, list));
    }

    [Fact]
    public void EnemiesCannotMoveOntoThePlayerOrAnotherEnemy()
    {
        var a = Slime(3, 4);
        var b = Slime(4, 4);
        var list = new[] { a, b };
        Assert.True(TileOccupancy.IsBlockedForEnemy(a, 5, 5, 5, 5, list));       // the player's tile
        Assert.True(TileOccupancy.IsBlockedForEnemy(a, 4, 4, 9, 9, list));       // another enemy
        Assert.False(TileOccupancy.IsBlockedForEnemy(a, 3, 4, 9, 9, list));      // its own tile is not an obstacle
        Assert.False(TileOccupancy.IsBlockedForEnemy(a, 3, 5, 9, 9, list));

        Assert.False(TileOccupancy.TryMoveEnemy(a, 5, 5, 5, 5, list));
        Assert.Equal((3, 4), (a.TileX, a.TileY));
        Assert.False(TileOccupancy.TryMoveEnemy(a, 4, 4, 9, 9, list));
        Assert.Equal((3, 4), (a.TileX, a.TileY));
        Assert.True(TileOccupancy.TryMoveEnemy(a, 3, 5, 9, 9, list));
        Assert.Equal((3, 5), (a.TileX, a.TileY));
    }

    [Fact]
    public void ADeadEnemyDoesNotBlockEnemiesAndCannotMove()
    {
        var a = Slime(3, 4);
        var dead = Slime(4, 4);
        dead.TakeDamage(10000);
        var list = new[] { a, dead };
        Assert.False(TileOccupancy.IsBlockedForEnemy(a, 4, 4, 9, 9, list));
        Assert.True(TileOccupancy.TryMoveEnemy(a, 4, 4, 9, 9, list));
        Assert.False(TileOccupancy.TryMoveEnemy(dead, 6, 6, 9, 9, list));
        Assert.Equal((4, 4), (dead.TileX, dead.TileY));
    }
}
