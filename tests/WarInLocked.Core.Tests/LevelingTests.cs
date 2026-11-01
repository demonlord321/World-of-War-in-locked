using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Tests;

public class LevelingTests
{
    private static Character Make(StatDistribution d = StatDistribution.Warrior) =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = d }.Create();

    // ---- XP costs

    [Fact]
    public void CostTableForTheFirst25Levels()
    {
        // cost to go from level L (index + 1) to L + 1
        int[] expected =
        {
            50, 100, 150, 200,                 // L1-4: +50
            300, 400, 500, 600, 700,           // L5-9: +100
            900, 1100, 1300, 1500, 1700,       // L10-19: +200
            1900, 2100, 2300, 2500, 2700,
            3100, 3500, 3900, 4300, 4700,      // L20-24: +400 ... (L25 -> 26 is 5100)
            5100,
        };
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], Leveling.CostToNext(i + 1));
    }

    [Theory]
    [InlineData(1, 50)]
    [InlineData(2, 100)]
    [InlineData(4, 200)]      // 4 -> 5 costs 200
    [InlineData(5, 300)]
    [InlineData(9, 700)]
    [InlineData(10, 900)]
    [InlineData(19, 2700)]    // 19 -> 20
    [InlineData(20, 3100)]
    [InlineData(29, 6700)]
    [InlineData(30, 7500)]    // step becomes 800
    [InlineData(31, 8300)]
    [InlineData(40, 16300)]   // 39 -> 40 is 7500 + 9*800 = 14700, then the step becomes 1600
    public void SpotChecks(int level, int cost)
    {
        Assert.Equal(cost, Leveling.CostToNext(level));
    }

    [Fact]
    public void StepDoublesEveryTenLevelsFromLevelTen()
    {
        Assert.Equal(50, Leveling.StepAt(2));
        Assert.Equal(50, Leveling.StepAt(4));
        Assert.Equal(100, Leveling.StepAt(5));
        Assert.Equal(100, Leveling.StepAt(9));
        Assert.Equal(200, Leveling.StepAt(10));
        Assert.Equal(200, Leveling.StepAt(19));
        Assert.Equal(400, Leveling.StepAt(20));
        Assert.Equal(800, Leveling.StepAt(30));
        Assert.Equal(1600, Leveling.StepAt(40));
    }

    [Fact]
    public void TotalXpIsTheSumOfTheCosts()
    {
        Assert.Equal(0, Leveling.TotalXpFor(1));
        Assert.Equal(50, Leveling.TotalXpFor(2));
        Assert.Equal(150, Leveling.TotalXpFor(3));
        Assert.Equal(500, Leveling.TotalXpFor(5));   // 50+100+150+200
    }

    // ---- character levelling

    [Fact]
    public void ANewCharacterIsLevelOneWithNothingEarned()
    {
        var c = Make();
        Assert.Equal(1, c.Level);
        Assert.Equal(0, c.Xp);
        Assert.Equal(0, c.XpIntoLevel);
        Assert.Equal(50, c.XpForNextLevel);
        Assert.Equal(0f, c.XpFraction);
        Assert.Equal(0, c.FreeStatPoints);
    }

    [Fact]
    public void FiftyXpGoesStraightToLevelTwoWithThreePoints()
    {
        var c = Make();
        Assert.Equal(1, c.GainXp(50));
        Assert.Equal(2, c.Level);
        Assert.Equal(3, c.FreeStatPoints);
        Assert.Equal(0, c.XpIntoLevel);
        Assert.Equal(100, c.XpForNextLevel);
    }

    [Fact]
    public void XpBarFractionTracksProgressInsideTheLevel()
    {
        var c = Make();
        c.GainXp(25);
        Assert.Equal(25, c.XpIntoLevel);
        Assert.Equal(0.5f, c.XpFraction, 3);
        c.GainXp(25 + 50);   // level 2 with 50/100
        Assert.Equal(2, c.Level);
        Assert.Equal(50, c.XpIntoLevel);
        Assert.Equal(0.5f, c.XpFraction, 3);
        Assert.Equal(75 + 25, c.Xp);
    }

    [Fact]
    public void OneBigGainCanLevelSeveralTimes()
    {
        var c = Make();
        int gained = c.GainXp(500);   // 50+100+150+200 = 500 -> level 5
        Assert.Equal(4, gained);
        Assert.Equal(5, c.Level);
        Assert.Equal(3 + 3 + 3 + 5, c.FreeStatPoints);   // levels 2, 3, 4 give 3; level 5 gives 5 instead
        Assert.Equal(0, c.XpIntoLevel);
        Assert.Equal(300, c.XpForNextLevel);
    }

    [Fact]
    public void GainingLittleXpGainsNoLevel()
    {
        var c = Make();
        Assert.Equal(0, c.GainXp(49));
        Assert.Equal(1, c.Level);
        Assert.Equal(0, c.GainXp(0));
        Assert.Equal(1, c.GainXp(1));
    }

    [Fact]
    public void EveryFifthLevelGivesFiveInsteadOfThree()
    {
        Assert.Equal(3, Leveling.StatPointsForReaching(2));
        Assert.Equal(3, Leveling.StatPointsForReaching(4));
        Assert.Equal(5, Leveling.StatPointsForReaching(5));
        Assert.Equal(3, Leveling.StatPointsForReaching(6));
        Assert.Equal(5, Leveling.StatPointsForReaching(10));
        Assert.Equal(5, Leveling.StatPointsForReaching(25));

        var c = Make();
        c.GainXp((int)Leveling.TotalXpFor(10));
        Assert.Equal(10, c.Level);
        // levels 2-10: seven levels of 3 (2,3,4,6,7,8,9) + two of 5 (5, 10)
        Assert.Equal(7 * 3 + 2 * 5, c.FreeStatPoints);
    }

    [Fact]
    public void LevelUpEventFiresOncePerLevelAndVersionChanges()
    {
        var c = Make();
        var seen = new List<int>();
        c.LeveledUp += l => seen.Add(l);
        int v = c.Version;
        c.GainXp(150);
        Assert.Equal(new[] { 2, 3 }, seen);
        Assert.NotEqual(v, c.Version);
    }

    // ---- stat points

    [Fact]
    public void AssignPointRaisesTheStatAndSpendsAPoint()
    {
        var c = Make();
        c.GainXp(50);
        int str = c.Stats.Strength;
        Assert.True(c.AssignPoint(StatType.Strength));
        Assert.Equal(str + 1, c.Stats.Strength);
        Assert.Equal(2, c.FreeStatPoints);
        Assert.Equal(1, c.AssignedPoints(StatType.Strength));
    }

    [Fact]
    public void AssignPointWithNoFreePointsDoesNothing()
    {
        var c = Make();
        int before = c.Stats.Strength;
        Assert.False(c.AssignPoint(StatType.Strength));
        Assert.Equal(before, c.Stats.Strength);
        Assert.Equal(0, c.FreeStatPoints);
    }

    [Fact]
    public void ConAndWisFeedTheMaximaAndCurrentValuesRiseByTheSameAmount()
    {
        var c = Make();                       // Warrior: Con 13 -> 230 HP, Wis 7 -> 35 EN
        c.GainXp(50);
        c.TakeDamage(100);                    // 130 / 230
        c.SpendEnergy(10);                    // 25 / 35

        Assert.True(c.AssignPoint(StatType.Constitution));
        Assert.Equal(240, c.MaxHealth);       // Con 14: 100 x 2.4
        Assert.Equal(140, c.Health);          // current rose by the +10 as well
        Assert.True(c.AssignPoint(StatType.Wisdom));
        Assert.Equal(40, c.MaxEnergy);
        Assert.Equal(30, c.Energy);
    }

    [Fact]
    public void FullHealthStaysFullWhenTheMaximumRises()
    {
        var c = Make();
        c.GainXp(50);
        c.AssignPoint(StatType.Constitution);
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(240, c.Health);
    }

    [Fact]
    public void StrengthPointsRaiseDamage()
    {
        var c = Make();
        c.GainXp(50);
        Assert.Equal(23, Combat.CombatController.BasicAttackDamage(c));   // unarmed: 10 x 2.3
        for (int i = 0; i < 3; i++) c.AssignPoint(StatType.Strength);   // Str 16
        Assert.Equal(26, Combat.CombatController.BasicAttackDamage(c));   // Str 16 -> 2.6 x 10
    }

    [Fact]
    public void UnassignGivesThePointBackAndLowersTheCurrentValuesToo()
    {
        var c = Make();
        c.GainXp(50);
        c.AssignPoint(StatType.Constitution);
        Assert.True(c.UnassignPoint(StatType.Constitution));
        Assert.Equal(13, c.Stats.Constitution);
        Assert.Equal(3, c.FreeStatPoints);
        Assert.Equal(230, c.MaxHealth);
        Assert.Equal(230, c.Health);
    }

    [Fact]
    public void CannotUnassignMoreThanWasAssigned()
    {
        var c = Make();
        c.GainXp(50);
        Assert.False(c.UnassignPoint(StatType.Luck));
        c.AssignPoint(StatType.Luck);
        Assert.True(c.UnassignPoint(StatType.Luck));
        Assert.False(c.UnassignPoint(StatType.Luck));
        Assert.Equal(3, c.FreeStatPoints);
    }

    [Fact]
    public void UnassigningNeverKillsAHurtCharacter()
    {
        var c = Make();
        c.GainXp(50);
        c.AssignPoint(StatType.Constitution);   // 240
        c.TakeDamage(239);                      // 1 left
        Assert.True(c.UnassignPoint(StatType.Constitution));
        Assert.Equal(1, c.Health);
        Assert.False(c.IsDead);
    }

    [Fact]
    public void ReachingAMilestoneInConGivesABigHealthJumpAndCurrentHealthFollows()
    {
        // Merchant: Con 9 -> 90 HP. The 10th point reaches the milestone: 100 x 2.0 = 200 (+110).
        var c = new CharacterCreation { Name = "M", Race = Race.Human, Distribution = StatDistribution.Merchant }.Create();
        Assert.Equal(90, c.MaxHealth);
        c.TakeDamage(40);                       // 50 / 90
        c.GainXp(50);
        Assert.True(c.AssignPoint(StatType.Constitution));
        Assert.Equal(200, c.MaxHealth);
        Assert.Equal(160, c.Health);            // 50 + 110
        Assert.True(c.UnassignPoint(StatType.Constitution));
        Assert.Equal(90, c.MaxHealth);
        Assert.Equal(50, c.Health);
    }
}
