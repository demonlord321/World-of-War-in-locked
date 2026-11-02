using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Tests;

public class NameValidatorTests
{
    [Theory]
    [InlineData("Aragorn")]
    [InlineData("Mary-Jane")]
    [InlineData("O'Brien")]
    [InlineData("Hero 2")]
    [InlineData("  Padded  ")]
    [InlineData("A")]
    public void AcceptsValidNames(string name)
    {
        Assert.Equal(NameError.None, NameValidator.Validate(name));
        Assert.True(NameValidator.IsValid(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyOrBlankNames(string? name)
    {
        Assert.Equal(NameError.Empty, NameValidator.Validate(name!));
        Assert.False(NameValidator.IsValid(name!));
    }

    [Fact]
    public void NoLengthLimit()   // #45: "for now no restriction on names"
    {
        Assert.Equal(NameError.None, NameValidator.Validate(new string('a', NameValidator.MaxLength + 1)));
        Assert.Equal(NameError.None, NameValidator.Validate(new string('a', 500)));
    }

    [Theory]
    [InlineData("Bad<Name>")]
    [InlineData("semi;colon")]
    [InlineData("Zoë")]
    [InlineData("Ünal 3rd, Esq.")]
    public void NoCharacterRestrictions(string name)
    {
        Assert.Equal(NameError.None, NameValidator.Validate(name));
    }

    [Fact]
    public void OnlyControlCharactersAreRefused()
    {
        Assert.Equal(NameError.InvalidCharacter, NameValidator.Validate("tab\there"));
        Assert.False(NameValidator.IsAllowedCharacter('\n'));
        Assert.True(NameValidator.IsAllowedCharacter('!'));
    }

    [Fact]
    public void NormalizeTrimsAndHandlesNull()
    {
        Assert.Equal("Bob", NameValidator.Normalize("  Bob "));
        Assert.Equal(string.Empty, NameValidator.Normalize(null!));
    }
}
