using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Tests;

public class CharacterCreationTests
{
    [Fact]
    public void StartsWithDefaultsAndCannotCreateWithoutAName()
    {
        var creation = new CharacterCreation();

        Assert.Equal(Race.Human, creation.Race);
        Assert.Equal(StatDistribution.Warrior, creation.Distribution);
        Assert.False(creation.CanCreate);
        Assert.Throws<InvalidOperationException>(() => creation.Create());
    }

    [Fact]
    public void StatsUpdateWhenTheDistributionChanges()
    {
        var creation = new CharacterCreation { Distribution = StatDistribution.Warrior };
        Assert.Equal(13, creation.Stats.Strength);

        creation.Distribution = StatDistribution.Mystic;
        Assert.Equal(7, creation.Stats.Strength);
        Assert.Equal(13, creation.Stats.Wisdom);

        creation.Distribution = StatDistribution.Merchant;
        Assert.Equal(13, creation.Stats.Luck);
    }

    [Fact]
    public void CreateBuildsACharacterWithTrimmedNameAndFinalStats()
    {
        var creation = new CharacterCreation
        {
            Name = "  Korra ",
            Race = Race.Elf,
            Distribution = StatDistribution.Merchant,
        };

        Assert.True(creation.CanCreate);
        var character = creation.Create();

        Assert.Equal("Korra", character.Name);
        Assert.Equal(Race.Elf, character.Race);
        Assert.Equal(StatDistribution.Merchant, character.Distribution);
        Assert.Equal(13, character.Stats.Charisma);
        Assert.Equal(13, character.Stats.Luck);
    }
}
