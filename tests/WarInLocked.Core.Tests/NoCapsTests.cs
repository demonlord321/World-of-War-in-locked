using WarInLocked.Core.Characters;
using WarInLocked.Core.Saving;

namespace WarInLocked.Core.Tests;

/// <summary>#45 Dyllan: "No level cap and for now no restriction on names. No stat cap either."</summary>
public class NoCapsTests
{
    private static Character Hero(string name = "Aragorn") =>
        new CharacterCreation { Name = name, Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    [Fact]
    public void LevelsKeepGoingPastAnyOldCap()
    {
        var c = Hero();
        c.GainXp((int)Leveling.TotalXpFor(150));
        Assert.Equal(150, c.Level);
        c.GainXp(1_000_000);
        Assert.True(c.Level >= 150);
        Assert.True(c.FreeStatPoints > 0);
    }

    [Fact]
    public void CostToNextNeverGoesNegativeOrOverflows()
    {
        for (int level = 1; level < 5000; level += 37)
            Assert.True(Leveling.CostToNext(level) > 0);
        Assert.True(Leveling.CostToNext(20000) > 0);
    }

    [Fact]
    public void AStatCanBePushedPastHundredWithFreePoints()
    {
        var c = Hero();
        c.GainXp((int)Leveling.TotalXpFor(120));
        int before = c.Stats.Strength;
        int spent = 0;
        while (c.AssignPoint(StatType.Strength)) spent++;
        Assert.True(spent > 100);
        Assert.Equal(before + spent, c.Stats.Strength);
        Assert.True(c.Stats.Strength > 100);
        Assert.Equal(0, c.FreeStatPoints);
    }

    [Fact]
    public void AHighLevelCharacterWithAWeirdLongNameSavesAndLoads()
    {
        string name = "Sir <Lancelot> \"the Great\"; Ünal " + new string('x', 80);
        var c = Hero(name);
        c.GainXp((int)Leveling.TotalXpFor(60));
        var back = SaveGame.FromJson(SaveGame.Capture(c, "forest-of-beginnings", 1, 1).ToJson()).ToCharacter();
        Assert.Equal(name, back.Name);
        Assert.Equal(c.Level, back.Level);
    }

    [Fact]
    public void ACharacterCanBeCreatedWithAnyNonEmptyName()
    {
        var create = new CharacterCreation { Name = "!!! 12 <>", Race = Race.Human, Distribution = StatDistribution.Warrior };
        Assert.True(create.CanCreate);
        Assert.Equal("!!! 12 <>", create.Create().Name);
    }
}
