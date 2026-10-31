using WarInLocked.Core.Characters;
using WarInLocked.Core.Dialogue;

namespace WarInLocked.Core.Tests;

public class DialogueTests
{
    private static Character Hero(string name = "Aragorn") =>
        new CharacterCreation { Name = name, Race = Race.Elf, Distribution = StatDistribution.Mystic }.Create();

    private static DialogueRunner Colonel(string name = "Aragorn") =>
        new DialogueRunner(DialogueLibrary.Get(DialogueLibrary.ColonelPowersId), Hero(name));

    [Fact]
    public void StartsWithTheGreetingAndThreeOptions()
    {
        var d = Colonel();

        Assert.False(d.IsEnded);
        Assert.Equal("Colonel Powers", d.Speaker);
        Assert.Equal("Greetings Maggot... Welcome to the first day of the start of your journey. Who are you...", d.Text);
        Assert.True(d.HasOptions);
        Assert.Equal(new[] { "My name is Aragorn", "Who the hell are you...", "Where the hell am I." }, d.Options);
    }

    [Fact]
    public void NameIsSubstitutedFromTheCharacter()
    {
        Assert.Equal("My name is Korra", Colonel("Korra").Options[0]);
        Assert.Equal("My name is Mary-Jane", Colonel("Mary-Jane").Options[0]);
    }

    [Fact]
    public void OptionTwoShowsItsReplyThenReturnsToTheThreeOptions()
    {
        var d = Colonel();
        var original = d.Options.ToArray();

        d.Choose(1);

        Assert.False(d.IsEnded);
        Assert.Equal("I suppose you should know. I am the one who shall teach you to fight...", d.Text);
        Assert.Equal(original, d.Options);
    }

    [Fact]
    public void OptionThreeShowsItsReplyThenReturnsToTheThreeOptions()
    {
        var d = Colonel();
        var original = d.Options.ToArray();

        d.Choose(2);

        Assert.False(d.IsEnded);
        Assert.Equal("This is the forest of beginnings. Where you learn to survive.", d.Text);
        Assert.Equal(original, d.Options);
    }

    [Fact]
    public void OptionsTwoAndThreeCanBeRepeatedAndMixedAnyNumberOfTimes()
    {
        var d = Colonel();
        foreach (int pick in new[] { 1, 2, 1, 1, 2, 2 })
        {
            d.Choose(pick);
            Assert.False(d.IsEnded);
            Assert.Equal(3, d.Options.Count);
        }
    }

    [Fact]
    public void OptionOneContinuesToTheFinalLineThenEnds()
    {
        var d = Colonel();

        d.Choose(0);

        Assert.False(d.IsEnded);
        Assert.False(d.HasOptions);
        Assert.Equal("I am Colonel Powers, your drill sergeant... Get ready maggot, it is time to learn to survive.", d.Text);

        d.Advance();

        Assert.False(d.IsEnded);
        Assert.Equal("I see you lack the skill to survive this world... Take this sword and hunt down 5 Slimes.", d.Text);

        d.Advance();

        Assert.True(d.IsEnded);
    }

    [Fact]
    public void OptionOneStillEndsAfterLoopingThroughTheOthers()
    {
        var d = Colonel();
        d.Choose(1);
        d.Choose(2);
        d.Choose(0);
        d.Advance();
        d.Advance();
        Assert.True(d.IsEnded);
    }

    [Fact]
    public void WrongCallsAndBadIndexesAreRejected()
    {
        var d = Colonel();
        Assert.Throws<InvalidOperationException>(() => d.Advance());      // has options
        Assert.Throws<ArgumentOutOfRangeException>(() => d.Choose(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => d.Choose(-1));

        d.Choose(0);
        Assert.Throws<InvalidOperationException>(() => d.Choose(0));      // no options now
        d.Advance();
        d.Advance();
        Assert.Throws<InvalidOperationException>(() => d.Advance());      // ended
        Assert.Throws<InvalidOperationException>(() => _ = d.Text);
    }

    [Fact]
    public void SubstituteFillsKnownPlaceholdersAndLeavesUnknownOnes()
    {
        var vars = DialogueRunner.VariablesFor(Hero("Aragorn"));
        Assert.Equal("Aragorn the Elf Mystic {rank}", DialogueRunner.Substitute("{name} the {race} {class} {rank}", vars));
    }

    [Fact]
    public void CustomGraphWithLoopBackAndLinearNodes()
    {
        var graph = new DialogueGraph("a", new[]
        {
            new DialogueNode("a", "Bob", "Hi {name}", options: new[]
            {
                new DialogueOption("Again", "a"),
                new DialogueOption("Next", "b"),
                new DialogueOption("Bye", null),
            }),
            new DialogueNode("b", "Bob", "Second", nextId: "c"),
            new DialogueNode("c", "Bob", "Third"),
        });

        var d = new DialogueRunner(graph, new Dictionary<string, string> { ["name"] = "Zed" });
        Assert.Equal("Hi Zed", d.Text);
        d.Choose(0);
        Assert.Equal("a", d.CurrentNodeId);        // looped back
        d.Choose(1);
        Assert.Equal("Second", d.Text);
        d.Advance();
        Assert.Equal("Third", d.Text);
        d.Advance();
        Assert.True(d.IsEnded);

        var quick = new DialogueRunner(graph, new Dictionary<string, string>());
        quick.Choose(2);                           // option with no next node ends it
        Assert.True(quick.IsEnded);
    }

    [Fact]
    public void GraphRejectsBrokenLinksAndMissingStart()
    {
        Assert.Throws<ArgumentException>(() => new DialogueGraph("a", new[] { new DialogueNode("a", "x", "y", nextId: "nope") }));
        Assert.Throws<ArgumentException>(() => new DialogueGraph("zzz", new[] { new DialogueNode("a", "x", "y") }));
        Assert.Throws<ArgumentException>(() => new DialogueGraph("a", new[] { new DialogueNode("a", "x", "y"), new DialogueNode("a", "x", "z") }));
    }

    [Fact]
    public void ColonelPowersIsRegisteredInTheLibrary()
    {
        Assert.True(DialogueLibrary.Exists("colonel-powers"));
        Assert.False(DialogueLibrary.Exists("nobody"));
        Assert.Throws<KeyNotFoundException>(() => DialogueLibrary.Get("nobody"));
    }
}
