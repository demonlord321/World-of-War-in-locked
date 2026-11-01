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
    [InlineData(1, 100, 10)]
    [InlineData(2, 125, 12)]
    [InlineData(3, 150, 15)]
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
        Assert.Throws<ArgumentOutOfRangeException>(() => Enemy.NewSlime(4));
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
    public void SlimesAreVulnerableToBothDamageTypesByThePlaceholderFactor()
    {
        var s = Enemy.NewSlime(2);
        Assert.Equal(1.25, s.PhysicalMultiplier);
        Assert.Equal(1.25, s.MagicalMultiplier);
        Assert.Equal(1.25, s.MultiplierFor(AbilityKind.Physical));
        Assert.Equal(1.25, s.MultiplierFor(AbilityKind.Magic));
        Assert.Equal("Vulnerable", s.ResistanceLabel(AbilityKind.Physical));
        Assert.Equal("Vulnerable", s.ResistanceLabel(AbilityKind.Magic));
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
        var slime = Enemy.NewSlime(1);
        // Slash: (10 + 5 + 5) x 2.3 x 1.25 = 57.5 -> 58 (rounding 46 x 1.25 = 57.5 would give the same here; basic attack differs):
        Assert.Equal(58, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition, 1, slime));
        Assert.Equal(43, CombatController.BasicAttackDamage(c, slime));   // 15 x 2.3 = 34.5 -> 35 first would give 44; once gives 43.125 -> 43
        Assert.Equal(35, CombatController.BasicAttackDamage(c));
    }

    [Fact]
    public void ADamagedSlimeKeepsItsLevelAndAttackAndDiesAtZero()
    {
        var s = Enemy.NewSlime(3);
        Assert.Equal(150, s.TakeDamage(150));
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
}
