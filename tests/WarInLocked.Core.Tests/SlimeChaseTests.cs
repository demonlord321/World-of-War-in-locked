using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#31: angered slimes chase the player one tile at a time.</summary>
public class SlimeChaseTests
{
    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    private static Enemy Slime(int x, int y, bool aggro = true)
    {
        var e = Enemy.NewSlime(1); e.TileX = x; e.TileY = y;
        if (aggro) e.Provoke();
        return e;
    }

    [Fact]
    public void PaceIsOneTilePerHalfSecond()
    {
        Assert.Equal(0.5, Enemy.ChaseStepSeconds);
    }

    [Fact]
    public void AnAggroSlimeOutOfReachStepsOneTileTowardThePlayerEveryHalfSecond()
    {
        var c = Hero();
        var s = Slime(10, 5);
        var list = new[] { s };
        c.Combat.UpdateEnemies(list, 0.4, 5, 5);
        Assert.Equal((10, 5), (s.TileX, s.TileY));          // not yet
        c.Combat.UpdateEnemies(list, 0.1, 5, 5);
        Assert.Equal((9, 5), (s.TileX, s.TileY));
        c.Combat.UpdateEnemies(list, 0.5, 5, 5);
        Assert.Equal((8, 5), (s.TileX, s.TileY));
        c.Combat.UpdateEnemies(list, 1.0, 5, 5);            // two steps in one call
        Assert.Equal((6, 5), (s.TileX, s.TileY));
    }

    [Fact]
    public void ItStopsAdjacentAndThenAttacksImmediately()
    {
        var c = Hero();
        var s = Slime(8, 5);
        var list = new[] { s };
        int dmg = c.Combat.UpdateEnemies(list, 0.5, 5, 5);   // 8 -> 7, still 2 away
        Assert.Equal(0, dmg);
        dmg = c.Combat.UpdateEnemies(list, 0.5, 5, 5);       // 7 -> 6: in reach, hits in the same call
        Assert.Equal((6, 5), (s.TileX, s.TileY));
        Assert.Equal(10, dmg);
        Assert.Equal(0, c.Combat.UpdateEnemies(list, 5.0, 5, 5));   // standing there, cooldown running, no further move
        Assert.Equal((6, 5), (s.TileX, s.TileY));
    }

    [Fact]
    public void DiagonalChaseUsesDiagonalSteps()
    {
        var c = Hero();
        var s = Slime(9, 9);
        c.Combat.UpdateEnemies(new[] { s }, 0.5, 5, 5);
        Assert.Equal((8, 8), (s.TileX, s.TileY));
        c.Combat.UpdateEnemies(new[] { s }, 0.5, 5, 5);
        Assert.Equal((7, 7), (s.TileX, s.TileY));
    }

    [Fact]
    public void PassiveDeadAndCalmedSlimesDoNotChase()
    {
        var c = Hero();
        var passive = Slime(10, 5, aggro: false);
        var dead = Slime(10, 6);
        dead.TakeDamage(10000);
        var calmed = Slime(10, 7);
        calmed.Calm();
        c.Combat.UpdateEnemies(new[] { passive, dead, calmed }, 5.0, 5, 5);
        Assert.Equal(10, passive.TileX);
        Assert.Equal(10, dead.TileX);
        Assert.Equal(10, calmed.TileX);
    }

    [Fact]
    public void ADeadPlayerIsNotChased()
    {
        var c = Hero();
        var s = Slime(10, 5);
        c.TakeDamage(c.MaxHealth);
        Assert.True(c.IsDead);
        c.Combat.UpdateEnemies(new[] { s }, 5.0, 5, 5);
        Assert.Equal(10, s.TileX);
    }

    [Fact]
    public void ChasersNeverStepOntoThePlayerOrAnotherSlime()
    {
        var c = Hero();
        var front = Slime(7, 5);                 // already 2 away, in the way of the slime behind it
        var behind = Slime(8, 5);
        var list = new[] { front, behind };
        for (int i = 0; i < 20; i++)
        {
            c.Combat.UpdateEnemies(list, 0.5, 5, 5);
            Assert.False(front.TileX == 5 && front.TileY == 5);
            Assert.False(behind.TileX == 5 && behind.TileY == 5);
            Assert.False(front.TileX == behind.TileX && front.TileY == behind.TileY);
        }
        Assert.Equal((6, 5), (front.TileX, front.TileY));   // in reach; the other one sidesteps around to a free neighbouring tile
        Assert.True(System.Math.Abs(behind.TileX - 5) <= 2 && System.Math.Abs(behind.TileY - 5) <= 2);
    }

    [Fact]
    public void WalkabilityCallbackKeepsChasersOutOfWallsAndAllowsSidestepping()
    {
        var c = Hero();
        var s = Slime(9, 5);
        var list = new[] { s };
        // A wall segment at x = 7, y 4..6 only (gap above and below).
        bool Walkable(int x, int y) => !(x == 7 && y >= 4 && y <= 6);
        var visited = new System.Collections.Generic.List<(int, int)>();
        for (int i = 0; i < 12; i++)
        {
            c.Combat.UpdateEnemies(list, 0.5, 5, 5, Walkable);
            visited.Add((s.TileX, s.TileY));
            Assert.True(Walkable(s.TileX, s.TileY));
        }
        // The slime walks up to the wall and tries to slide around it (it is dumb: it only sidesteps when blocked); at least it never enters it.
        Assert.DoesNotContain(visited, v => v.Item1 == 7 && v.Item2 >= 4 && v.Item2 <= 6);
        Assert.Equal((8, 5), (s.TileX, s.TileY));   // stuck against the wall: straight into it, sidesteps (8,4),(8,6) are blocked diagonally beside the wall
    }

    [Fact]
    public void DiagonalStepsDoNotCutWallCorners()
    {
        var c = Hero();
        var s = Slime(7, 7);
        // (6,7) is a wall; the diagonal (6,6) is open but passes beside the wall corner.
        bool Walkable(int x, int y) => !(x == 6 && y == 7);
        c.Combat.UpdateEnemies(new[] { s }, 0.5, 3, 3, Walkable);
        Assert.NotEqual((6, 6), (s.TileX, s.TileY));
        Assert.Equal((7, 6), (s.TileX, s.TileY));   // fell back to the straight step toward the player
    }

    [Fact]
    public void AStuckChaserDoesNotStoreUpABurstOfSteps()
    {
        var c = Hero();
        var s = Slime(8, 5);
        var list = new[] { s };
        c.Combat.UpdateEnemies(list, 10.0, 5, 5, (x, y) => false);   // nowhere to go
        Assert.Equal((8, 5), (s.TileX, s.TileY));
        c.Combat.UpdateEnemies(list, 0.0, 5, 5);                      // now free: exactly one step, not twenty
        Assert.Equal((7, 5), (s.TileX, s.TileY));
    }

    [Fact]
    public void OutOfReachSlimeThatCannotMoveStillDoesNotAttackAndStaysReady()
    {
        var c = Hero();
        var s = Slime(8, 5);
        for (int i = 0; i < 5; i++) Assert.Equal(0, c.Combat.UpdateEnemies(new[] { s }, 1.0, 5, 5, (x, y) => false));
        Assert.Equal(0, s.AttackCooldownRemaining);
        Assert.Equal(c.MaxHealth, c.Health);
    }

    [Fact]
    public void ProvokingBySwordThenWalkingAwayMakesTheSlimeFollow()
    {
        var c = Hero();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        var s = Slime(6, 5, aggro: false);
        var list = new[] { s };
        c.Combat.BasicAttack(5, 5, list);
        Assert.True(s.IsAggro);
        c.Combat.UpdateEnemies(list, 0.016, 5, 5);                     // hits at once
        c.Combat.UpdateEnemies(list, 1.0, 12, 5);                      // player runs away: slime follows (2 steps)
        Assert.Equal((8, 5), (s.TileX, s.TileY));
    }

    [Fact]
    public void WandererCanBeToldWhereTheChaseMovedIt()
    {
        var w = new Wanderer(4, 4, new System.Random(1));
        w.GlideTo(5, 4);
        Assert.Equal((5, 4), (w.TileX, w.TileY));
        Assert.Equal(4f, w.X);                  // starts the glide where it was
        w.Glide(0.2f);                          // 0.2 s * 2.5 tiles/s = half a tile
        Assert.Equal(4.5f, w.X, 3);
        w.Glide(1f);
        Assert.Equal(5f, w.X);
        w.GlideTo(5, 4);                        // same tile: no-op
        Assert.Equal(5f, w.X);
    }
}
