using WarInLocked.Core.Combat;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#57 day and night: night monsters are 10 levels stronger; safe zones.</summary>
public class DayNightTests
{
    private static GameClock At(double hoursAfterStart)
    {
        var c = new GameClock();
        c.Advance(hoursAfterStart);
        return c;
    }

    [Theory]
    [InlineData(1, 50, 10)]
    [InlineData(2, 75, 12)]
    [InlineData(3, 100, 15)]
    [InlineData(4, 125, 18)]
    [InlineData(5, 150, 21)]
    [InlineData(6, 175, 24)]
    [InlineData(10, 275, 36)]
    [InlineData(11, 300, 39)]
    [InlineData(13, 350, 45)]
    [InlineData(14, 375, 48)]
    [InlineData(15, 400, 51)]
    public void SlimeStatsContinueThePatternUpToLevelFifteen(int level, int hp, int attack)
    {
        var s = Enemy.NewSlime(level);
        Assert.Equal(level, s.Level);
        Assert.Equal(hp, s.MaxHp);
        Assert.Equal(attack, s.Attack);
        Assert.Equal(1.25, s.PhysicalMultiplier);
        Assert.Equal(1.25, s.MagicalMultiplier);
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewSlime(16));
    }

    [Fact]
    public void ByDayMonstersKeepTheirBaseLevelAndByNightTheyGetTenMore()
    {
        var day = At(0);          // 8 am
        var night = At(12);       // 8 pm
        Assert.Equal(3, Enemy.LevelFor(3, day));
        Assert.Equal(13, Enemy.LevelFor(3, night));

        var d = Enemy.NewSlime(2, day);
        Assert.Equal(2, d.Level);
        Assert.False(d.IsNightEmpowered);

        var n = Enemy.NewSlime(2, night);
        Assert.Equal(12, n.Level);
        Assert.Equal(2, n.BaseLevel);
        Assert.True(n.IsNightEmpowered);
        Assert.Equal(Enemy.SlimeHealthFor(12), n.MaxHp);
        Assert.Equal(Enemy.SlimeAttackFor(12), n.Attack);
    }

    [Fact]
    public void NightLevelsForTheForestAndThePath()
    {
        var night = At(12);
        var rng = new Random(5);
        for (int i = 0; i < 300; i++)
        {
            Assert.InRange(Enemy.NewRandomSlime(rng, Enemy.MinSlimeLevel, Enemy.MaxSlimeLevel, night).Level, 11, 13);
            Assert.InRange(Enemy.NewRandomSlime(rng, Enemy.PathMinSlimeLevel, Enemy.PathMaxSlimeLevel, night).Level, 14, 15);
            Assert.InRange(Enemy.NewRandomSlime(rng, Enemy.PathMinSlimeLevel, Enemy.PathMaxSlimeLevel, At(0)).Level, 4, 5);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewRandomSlime(rng, 1, 6, night));
    }

    [Fact]
    public void ALiveSlimeKeepsItsLevelWhenNightFalls_OnlyNewSpawnsUseTheTime()
    {
        var clock = At(10);                            // 6 pm: day (placeholder gap)
        var live = Enemy.NewSlime(3, clock);
        clock.Advance(2);                              // 8 pm: night
        Assert.True(clock.IsNight);
        Assert.Equal(3, live.Level);                   // unchanged
        Assert.Equal(13, Enemy.NewSlime(3, clock).Level);   // a respawn now is a night slime
        clock.AdvanceToMorning();
        Assert.Equal(3, Enemy.NewSlime(3, clock).Level);
    }

    [Fact]
    public void ForestAndPathAreWildAndTheEncampmentIsSafe()
    {
        Assert.Equal("adventurers-encampment", MapIds.AdventurersEncampment);
        Assert.True(MapIds.IsSafeZone(MapIds.AdventurersEncampment));
        Assert.False(MapIds.AllowsMonsters(MapIds.AdventurersEncampment));
        Assert.False(MapIds.IsSafeZone(MapIds.Forest));
        Assert.True(MapIds.AllowsMonsters(MapIds.Path));
        Assert.False(MapIds.IsSafeZone("somewhere-new"));
        Assert.False(MapIds.IsSafeZone(null));
    }
}
