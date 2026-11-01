using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;

namespace WarInLocked.Core.Tests;

/// <summary>#33: the Adventurer title's +2 never takes a stat up to or past its next milestone (10, 25, 50).</summary>
public class TitleMilestoneTests
{
    private static Character WithStat(int value)
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(value, 5, 5, 5, 5, 5, 5, 5));
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        return c;
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(7, 2)]
    [InlineData(8, 1)]     // 8 -> 9
    [InlineData(9, 0)]     // stays 9
    [InlineData(10, 2)]    // 10 -> 12
    [InlineData(11, 2)]
    [InlineData(22, 2)]
    [InlineData(23, 1)]    // 23 -> 24
    [InlineData(24, 0)]    // stays 24
    [InlineData(25, 2)]    // 25 -> 27
    [InlineData(47, 2)]
    [InlineData(48, 1)]
    [InlineData(49, 0)]
    [InlineData(50, 2)]
    [InlineData(80, 2)]    // no milestone left
    public void BonusIsTwoButStopsOneShortOfTheNextMilestone(int baseValue, int bonus)
    {
        Assert.Equal(bonus, StatModifier.LimitedBonus(baseValue, 2));
        var c = WithStat(baseValue);
        Assert.Equal(bonus, c.TitleBonus(StatType.Strength));
        Assert.Equal(baseValue + bonus, c.EffectiveStats.Strength);
        Assert.Equal(baseValue, c.Stats.Strength);
    }

    [Fact]
    public void EachStatIsLimitedOnItsOwn()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(8, 9, 10, 23, 24, 25, 1, 49));
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        var e = c.EffectiveStats;
        Assert.Equal(new[] { 9, 9, 12, 24, 24, 27, 3, 49 },
            new[] { e.Strength, e.Dexterity, e.Constitution, e.Wisdom, e.Intelligence, e.Perception, e.Charisma, e.Luck });
    }

    [Fact]
    public void TheModifierNeverJumpsAMilestoneThroughTheTitle()
    {
        var c = WithStat(8);
        Assert.Equal(0.9, c.Modifier(StatType.Strength), 10);   // not 2.0
        var d = WithStat(9);
        Assert.Equal(0.9, d.Modifier(StatType.Strength), 10);
    }

    [Fact]
    public void InvestingPointsToReachTheMilestoneBringsTheFullBonusBack()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(8, 5, 5, 5, 5, 5, 5, 5));
        while (c.FreeStatPoints < 2) c.GainXp(c.XpForNextLevel);
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        Assert.Equal(9, c.EffectiveStats.Strength);                 // 8 + 1
        Assert.True(c.AssignPoint(StatType.Strength));
        Assert.Equal(9, c.Stats.Strength);
        Assert.Equal(9, c.EffectiveStats.Strength);                 // invest to 9: stays 9
        Assert.True(c.AssignPoint(StatType.Strength));
        Assert.Equal(10, c.Stats.Strength);
        Assert.Equal(12, c.EffectiveStats.Strength);                // invest to 10: +2 again
        Assert.Equal(2.2, c.Modifier(StatType.Strength), 10);
        Assert.True(c.UnassignPoint(StatType.Strength));
        Assert.Equal(9, c.EffectiveStats.Strength);
    }

    [Fact]
    public void UnwornTitlesPreviewTheLimitedBonus()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(8, 9, 10, 5, 5, 5, 5, 5));
        Assert.Equal(1, c.TitleBonusFor(TitleLibrary.Adventurer, StatType.Strength));
        Assert.Equal(0, c.TitleBonusFor(TitleLibrary.Adventurer, StatType.Dexterity));
        Assert.Equal(2, c.TitleBonusFor(TitleLibrary.Adventurer, StatType.Constitution));
        Assert.Equal(0, c.TitleBonus(StatType.Strength));           // not worn
        Assert.Equal(0, c.TitleBonusFor(null, StatType.Strength));
    }

    [Fact]
    public void MaxHealthFollowsTheLimitedBonus()
    {
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, new StatBlock(5, 5, 9, 5, 5, 5, 5, 5));
        int before = c.MaxHealth;
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        Assert.Equal(before, c.MaxHealth);                          // Constitution 9 gets nothing
        Assert.True(c.UnequipTitle());
        Assert.Equal(before, c.MaxHealth);
    }

    [Fact]
    public void NextMilestoneAndLimitedBonusHelpers()
    {
        Assert.Equal(10, StatModifier.NextMilestone(0));
        Assert.Equal(10, StatModifier.NextMilestone(9));
        Assert.Equal(25, StatModifier.NextMilestone(10));
        Assert.Equal(50, StatModifier.NextMilestone(25));
        Assert.Equal(0, StatModifier.NextMilestone(50));
        Assert.Equal(0, StatModifier.LimitedBonus(5, 0));
        Assert.Equal(5, StatModifier.LimitedBonus(1, 5));
        Assert.Equal(8, StatModifier.LimitedBonus(1, 8));           // cap is next - 1 - base = 8
    }

    [Fact]
    public void TheDescriptionExplainsTheRule()
    {
        Assert.Contains("never takes a stat up to or past its next milestone", TitleLibrary.Adventurer.Description);
        Assert.Contains("a stat of 8 gets +1 (9)", TitleLibrary.Adventurer.Description);
    }
}
