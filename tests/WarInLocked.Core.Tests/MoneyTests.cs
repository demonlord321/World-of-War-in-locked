using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Tests;

public class MoneyTests
{
    [Fact]
    public void ConstantsAreHundredAndTenThousand()
    {
        Assert.Equal(100, Money.CopperPerSilver);
        Assert.Equal(10000, Money.CopperPerGold);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(99, 0, 0, 99)]
    [InlineData(100, 0, 1, 0)]
    [InlineData(9999, 0, 99, 99)]
    [InlineData(10000, 1, 0, 0)]
    [InlineData(12345, 1, 23, 45)]
    [InlineData(10005, 1, 0, 5)]
    [InlineData(250000, 25, 0, 0)]
    public void SplitsATotalIntoParts(long total, long gold, int silver, int copper)
    {
        Assert.Equal(gold, Money.Gold(total));
        Assert.Equal(silver, Money.Silver(total));
        Assert.Equal(copper, Money.CopperPart(total));
        Assert.Equal(total, Money.FromParts(gold, silver, copper));
    }

    [Theory]
    [InlineData(0, "0c")]
    [InlineData(5, "5c")]
    [InlineData(50, "50c")]
    [InlineData(99, "99c")]
    [InlineData(100, "1s 0c")]
    [InlineData(200, "2s 0c")]
    [InlineData(2345, "23s 45c")]
    [InlineData(9999, "99s 99c")]
    [InlineData(10000, "1g 0s 0c")]
    [InlineData(10005, "1g 0s 5c")]
    [InlineData(12345, "1g 23s 45c")]
    [InlineData(1234567, "123g 45s 67c")]
    public void FormatOmitsLeadingZeroUnitsButKeepsInnerAndTrailingOnes(long total, string expected)
    {
        Assert.Equal(expected, Money.Format(total));
    }

    [Fact]
    public void FromPartsCarriesOverAndRejectsNegatives()
    {
        Assert.Equal(15000, Money.FromParts(0, 150, 0));
        Assert.Equal(101, Money.FromParts(0, 0, 101));
        Assert.Equal(0, Money.FromParts(0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromParts(-1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromParts(0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromParts(0, 0, -1));
    }

    [Fact]
    public void NegativeTotalsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Format(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Gold(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Silver(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.CopperPart(-1));
    }

    [Fact]
    public void CharacterCopperIsTheTotalAndFormatsThroughMoney()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.GainCopper((int)Money.FromParts(1, 23, 45));   // Character.Copper is an int total (max about 214,000 gold)
        Assert.Equal(12345, c.Copper);
        Assert.Equal("1g 23s 45c", Money.Format(c.Copper));
        Assert.True(c.SpendCopper(3 * Money.CopperPerSilver));
        Assert.Equal("1g 20s 45c", Money.Format(c.Copper));
    }
}
