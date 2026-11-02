using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#24: slime levels 1-3 and the resistance multipliers.</summary>
public class SlimeTests
{
    [Theory]
    [InlineData(1, 50, 10)]
    [InlineData(2, 75, 12)]
    [InlineData(3, 100, 15)]
    [InlineData(4, 125, 18)]
    [InlineData(5, 150, 21)]
    public void SlimeStatsPerLevel(int level, int hp, int attack)
    {
        var s = Enemy.NewSlime(level);
        Assert.Equal(level, s.Level);
        Assert.Equal(hp, s.MaxHealth);
        Assert.Equal(hp, s.MaxHp);
        Assert.Equal(hp, s.Hp);
        Assert.Equal(attack, s.Attack);
        Assert.Equal(Enemy.SlimeType, s.Type);
        Assert.False(s.IsDamaged);
    }

    [Fact]
    public void DefaultSlimeIsLevelOneAndBadLevelsAreRejected()
    {
        Assert.Equal(1, Enemy.NewSlime().Level);
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewSlime(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewSlime(16));
    }

    [Fact]
    public void RandomSlimesAreUniformOverLevelsOneToThreeAndRepeatableWithASeed()
    {
        var a = new Random(1234);
        var b = new Random(1234);
        var counts = new int[4];
        for (int i = 0; i < 3000; i++)
        {
            var s = Enemy.NewRandomSlime(a);
            Assert.Equal(Enemy.NewRandomSlime(b).Level, s.Level);   // same seed, same sequence
            Assert.InRange(s.Level, 1, 3);
            Assert.Equal(Enemy.SlimeHealthFor(s.Level), s.MaxHealth);
            counts[s.Level]++;
        }
        for (int level = 1; level <= 3; level++) Assert.InRange(counts[level], 900, 1100);   // about 1000 each
    }

    [Fact]
    public void SlimesTakeStandardDamageFromBothTypesForNow()
    {
        // Dyllan, 1 Nov: standard damage for now, weaknesses may come back later (the multiplier mechanism stays in Enemy).
        var s = Enemy.NewSlime(2);
        Assert.Equal(1.0, s.PhysicalMultiplier);
        Assert.Equal(1.0, s.MagicalMultiplier);
        Assert.Equal(1.0, s.MultiplierFor(AbilityKind.Physical));
        Assert.Equal(1.0, s.MultiplierFor(AbilityKind.Magic));
        Assert.Equal("Normal", s.ResistanceLabel(AbilityKind.Physical));
        Assert.Equal("Normal", s.ResistanceLabel(AbilityKind.Magic));
    }

    [Fact]
    public void TheMultiplierMechanismStillWorksForALaterWeakness()
    {
        var c = new CharacterCreation { Name = "W", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        var weak = new Enemy("weakling", 100, 1, 0, 1.25, 1.25);
        Assert.Equal(33, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition, 1, weak));   // 20 x 1.3 x 1.25 = 32.5 -> 33
        Assert.Equal(24, CombatController.BasicAttackDamage(c, weak));                                   // 15 x 1.3 x 1.25 = 24.375 -> 24
    }

    [Fact]
    public void PlainEnemiesHaveNormalResistancesAndResistantOnesAreLabelled()
    {
        var plain = new Enemy("dummy", 10);
        Assert.Equal(1.0, plain.PhysicalMultiplier);
        Assert.Equal("Normal", plain.ResistanceLabel(AbilityKind.Physical));
        var tough = new Enemy("armoured", 10, 1, 0, 0.5, 1.0);
        Assert.Equal("Resistant", tough.ResistanceLabel(AbilityKind.Physical));
        Assert.Equal("Normal", tough.ResistanceLabel(AbilityKind.Magic));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Enemy("bad", 10, 1, 0, -1.0, 1.0));
    }

    [Fact]
    public void TheMultiplierIsAppliedBeforeRoundingOnce()
    {
        var c = new CharacterCreation { Name = "W", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        var weak = new Enemy("weakling", 100, 1, 0, 1.25, 1.25);
        // Slash: (10 + 5 + 5) x 1.3 x 1.25 = 32.5 -> 33 (rounding 26 x 1.25 = 32.5 would give the same here; basic attack differs):
        Assert.Equal(33, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition, 1, weak));
        Assert.Equal(24, CombatController.BasicAttackDamage(c, weak));   // 15 x 1.3 = 19.5 -> 20 first would give 25; once gives 24.375 -> 24
        Assert.Equal(20, CombatController.BasicAttackDamage(c));
        Assert.Equal(20, CombatController.BasicAttackDamage(c, Enemy.NewSlime(1)));   // a slime takes standard damage
    }

    [Fact]
    public void ADamagedSlimeKeepsItsLevelAndAttackAndDiesAtZero()
    {
        var s = Enemy.NewSlime(3);
        Assert.Equal(100, s.TakeDamage(100));
        Assert.True(s.IsDead);
        Assert.Equal(3, s.Level);
        Assert.Equal(15, s.Attack);
    }

    [Fact]
    public void RespawnQueueStillBringsSlimeSpawnPointsBack()
    {
        var q = new RespawnQueue();
        q.Schedule(3, 4);
        Assert.Empty(q.Update(5));
        var due = q.Update(6);
        Assert.Single(due);
        // the game rerolls the level for the new slime
        var rng = new Random(7);
        Assert.InRange(Enemy.NewRandomSlime(rng).Level, 1, 3);
    }

    [Fact]
    public void PathSlimesAreLevelFourAndFiveWithStandardDamageAndTheSameInterval()
    {
        foreach (int level in new[] { 4, 5 })
        {
            var s = Enemy.NewSlime(level);
            Assert.Equal(1.0, s.PhysicalMultiplier);
            Assert.Equal(1.0, s.MagicalMultiplier);
            Assert.False(s.IsAggro);                                   // passive until hit
            s.TakeDamage(1);
            Assert.Equal(0, s.AttackCooldownRemaining);                // aggro'd and ready
        }
        Assert.Equal(6.0, Enemy.AttackIntervalSeconds);
    }

    [Fact]
    public void LevelRangeIsPerMapAndTheForestCallIsUnchanged()
    {
        var rng = new Random(77);
        var path = new int[6];
        var forest = new int[6];
        for (int i = 0; i < 2000; i++)
        {
            path[Enemy.NewRandomSlime(rng, Enemy.PathMinSlimeLevel, Enemy.PathMaxSlimeLevel).Level]++;
            forest[Enemy.NewRandomSlime(rng).Level]++;                 // old call: 1-3
        }
        Assert.Equal(0, path[1] + path[2] + path[3]);
        Assert.InRange(path[4], 850, 1150);
        Assert.InRange(path[5], 850, 1150);
        Assert.Equal(0, forest[4] + forest[5]);
        Assert.Equal(2000, forest[1] + forest[2] + forest[3]);

        Assert.Equal(5, Enemy.NewRandomSlime(rng, 5, 5).Level);        // a single level works
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewRandomSlime(rng, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewRandomSlime(rng, 3, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewRandomSlime(rng, 5, 4));
    }
}
