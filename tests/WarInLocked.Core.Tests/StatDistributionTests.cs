using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Tests;

public class StatDistributionTests
{
    [Fact]
    public void BaseStatsAreAllEight()
    {
        Assert.Equal(8, StatDistributions.BaseStat);
        var stats = StatDistributions.BaseStats;
        foreach (var stat in StatTypes.All)
            Assert.Equal(8, stats[stat]);
        Assert.Equal(64, stats.Total);
    }

    [Fact]
    public void StatOrderMatchesTheIssue()
    {
        Assert.Equal(
            new[]
            {
                StatType.Strength, StatType.Dexterity, StatType.Constitution, StatType.Wisdom,
                StatType.Intelligence, StatType.Perception, StatType.Charisma, StatType.Luck,
            },
            StatTypes.All);
    }

    // Str, Dex, Con, Wis, Int, Per, Cha, Luck: base 8 plus Dyllan's bonuses.
    [Theory]
    [InlineData(StatDistribution.Warrior, 13, 13, 13, 7, 7, 10, 8, 8)]
    [InlineData(StatDistribution.Mystic, 7, 9, 7, 13, 13, 13, 11, 8)]
    [InlineData(StatDistribution.Merchant, 8, 9, 9, 9, 9, 9, 13, 13)]
    public void FinalStatsForEachDistribution(
        StatDistribution distribution, int str, int dex, int con, int wis, int intel, int per, int cha, int luck)
    {
        var stats = StatDistributions.Calculate(Race.Human, distribution);

        Assert.Equal(str, stats.Strength);
        Assert.Equal(dex, stats.Dexterity);
        Assert.Equal(con, stats.Constitution);
        Assert.Equal(wis, stats.Wisdom);
        Assert.Equal(intel, stats.Intelligence);
        Assert.Equal(per, stats.Perception);
        Assert.Equal(cha, stats.Charisma);
        Assert.Equal(luck, stats.Luck);
    }

    [Theory]
    [InlineData(StatDistribution.Warrior, 5, 5, 5, -1, -1, 2, 0, 0)]
    [InlineData(StatDistribution.Mystic, -1, 1, -1, 5, 5, 5, 3, 0)]
    [InlineData(StatDistribution.Merchant, 0, 1, 1, 1, 1, 1, 5, 5)]
    public void BonusTablesMatchTheIssue(
        StatDistribution distribution, int str, int dex, int con, int wis, int intel, int per, int cha, int luck)
    {
        var bonus = StatDistributions.GetBonus(distribution);

        Assert.Equal(new[] { str, dex, con, wis, intel, per, cha, luck },
                     StatTypes.All.Select(s => bonus[s]).ToArray());
    }

    [Fact]
    public void BonusTotalsAreAsSpecified()
    {
        // Mystic is +17 and the others +15 on purpose; this is an open question in QUESTIONS.md.
        Assert.Equal(15, StatDistributions.GetBonus(StatDistribution.Warrior).Total);
        Assert.Equal(17, StatDistributions.GetBonus(StatDistribution.Mystic).Total);
        Assert.Equal(15, StatDistributions.GetBonus(StatDistribution.Merchant).Total);
    }

    [Theory]
    [InlineData(Race.Human)]
    [InlineData(Race.Elf)]
    [InlineData(Race.Dwarf)]
    public void RaceHasNoStatEffectYet(Race race)
    {
        Assert.Equal(0, StatDistributions.GetRaceModifier(race).Total);
        foreach (var distribution in Enum.GetValues<StatDistribution>())
        {
            var withRace = StatDistributions.Calculate(race, distribution);
            var human = StatDistributions.Calculate(Race.Human, distribution);
            Assert.Equal(human.ToString(), withRace.ToString());
        }
    }

    [Fact]
    public void StatBlockPlusAddsEachStat()
    {
        var sum = new StatBlock(1, 2, 3, 4, 5, 6, 7, 8).Plus(StatBlock.Uniform(10));
        Assert.Equal(new StatBlock(11, 12, 13, 14, 15, 16, 17, 18).ToString(), sum.ToString());
    }
}
