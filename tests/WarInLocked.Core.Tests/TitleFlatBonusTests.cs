using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;

namespace WarInLocked.Core.Tests;

/// <summary>#33: the Adventurer title gives a flat +2 to every stat. Dyllan: "With this Chang we can remove the titles restriction, the flat bonus is no longer going to cause such a drastic jump".</summary>
public class TitleFlatBonusTests
{
    private static Character WithStat(int value)
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(value, 5, 5, 5, 5, 5, 5, 5));
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        return c;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(25)]
    [InlineData(49)]
    [InlineData(50)]
    [InlineData(80)]
    public void BonusIsAlwaysAFlatTwo(int baseValue)
    {
        var c = WithStat(baseValue);
        Assert.Equal(2, c.TitleBonus(StatType.Strength));
        Assert.Equal(baseValue + 2, c.EffectiveStats.Strength);
        Assert.Equal(baseValue, c.Stats.Strength);
    }

    [Fact]
    public void EveryStatGetsTwo()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(8, 9, 10, 23, 24, 25, 1, 49));
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        var e = c.EffectiveStats;
        Assert.Equal(new[] { 10, 11, 12, 25, 26, 27, 3, 51 },
            new[] { e.Strength, e.Dexterity, e.Constitution, e.Wisdom, e.Intelligence, e.Perception, e.Charisma, e.Luck });
    }

    [Fact]
    public void TheModifierGoesUpByPointTwoWithTheTitle()
    {
        Assert.Equal(1.0, WithStat(8).Modifier(StatType.Strength), 10);   // 8 + 2 = 10
        Assert.Equal(1.1, WithStat(9).Modifier(StatType.Strength), 10);   // 9 + 2 = 11, no jump (#46)
    }

    [Fact]
    public void InvestedPointsAddToTheBonusAndUnassigningTakesThemOff()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(8, 5, 5, 5, 5, 5, 5, 5));
        while (c.FreeStatPoints < 2) c.GainXp(c.XpForNextLevel);
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        Assert.Equal(10, c.EffectiveStats.Strength);
        Assert.True(c.AssignPoint(StatType.Strength));
        Assert.Equal(11, c.EffectiveStats.Strength);
        Assert.True(c.AssignPoint(StatType.Strength));
        Assert.Equal(12, c.EffectiveStats.Strength);
        Assert.Equal(1.2, c.Modifier(StatType.Strength), 10);
        Assert.True(c.UnassignPoint(StatType.Strength));
        Assert.Equal(11, c.EffectiveStats.Strength);
    }

    [Fact]
    public void UnwornTitlesPreviewTheFullBonus()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(8, 9, 10, 5, 5, 5, 5, 5));
        Assert.Equal(2, c.TitleBonusFor(TitleLibrary.Adventurer, StatType.Strength));
        Assert.Equal(2, c.TitleBonusFor(TitleLibrary.Adventurer, StatType.Dexterity));
        Assert.Equal(2, c.TitleBonusFor(TitleLibrary.Adventurer, StatType.Constitution));
        Assert.Equal(0, c.TitleBonus(StatType.Strength));           // not worn
        Assert.Equal(0, c.TitleBonusFor(null, StatType.Strength));
    }

    [Fact]
    public void MaxHealthFollowsTheFullBonus()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(5, 5, 9, 5, 5, 5, 5, 5));
        int before = c.MaxHealth;                                   // 90
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        Assert.Equal(before + 20, c.MaxHealth);                     // Constitution 11: 110
        Assert.True(c.UnequipTitle());
        Assert.Equal(before, c.MaxHealth);
    }

    [Fact]
    public void NextMilestoneStillExistsForALaterMilestoneBonus()
    {
        Assert.Equal(10, StatModifier.NextMilestone(0));
        Assert.Equal(10, StatModifier.NextMilestone(9));
        Assert.Equal(25, StatModifier.NextMilestone(10));
        Assert.Equal(50, StatModifier.NextMilestone(25));
        Assert.Equal(0, StatModifier.NextMilestone(50));
    }

    [Fact]
    public void TheDescriptionIsJustDyllansTextWithNoCapRule()
    {
        Assert.Equal("You have completed the tutorial. The Adventurer is one who is prepared for the upcoming adventure.", TitleLibrary.Adventurer.Description);
        Assert.DoesNotContain("milestone", TitleLibrary.Adventurer.Description);
    }
}
