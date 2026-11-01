using WarInLocked.Core.Characters;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Interaction;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#55 Barracks Master.</summary>
public class BarracksMasterTests
{
    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    private static TileMap Camp() =>
        TileMap.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../src/WarInLocked.Game/Assets/Maps/adventurers-encampment.txt")));

    [Fact]
    public void DialogueIsDyllansWelcomeThenRestAndSetAsHome()
    {
        var d = BarracksMaster.CreateDialogue(Hero());
        Assert.Equal("Barracks Master", d.Speaker);
        Assert.Equal("Welcome adventurer to the Adventurers encampment. Until you are prepared for the pilgrimage you may rest up here and train.", d.Text);
        Assert.Equal(new[] { "Rest", "Set as home", "Leave" }, d.Options.ToArray());
        Assert.Equal("barracks-master", BarracksMaster.NpcId);
        Assert.True(DialogueLibrary.Exists("barracks-master"));
        Assert.Equal("welcome", DialogueLibrary.Get("barracks-master").StartId);
    }

    [Fact]
    public void SleepOptionOnlyAppearsAtNight()
    {
        var c = Hero();
        Assert.DoesNotContain("Sleep until morning", BarracksMaster.CreateDialogue(c).Options.ToArray());
        c.Clock.Advance(11);   // 7 pm
        Assert.True(c.Clock.IsNight);
        Assert.Equal(new[] { "Rest", "Sleep until morning", "Set as home", "Leave" }, BarracksMaster.CreateDialogue(c).Options.ToArray());
    }

    [Fact]
    public void ChoosingAnOptionEndsTheDialogueWithItsAction()
    {
        var c = Hero();
        c.Clock.Advance(11);
        string[] expected = { BarracksActions.Rest, BarracksActions.Sleep, BarracksActions.SetAsHome, null };
        for (int i = 0; i < expected.Length; i++)
        {
            var d = BarracksMaster.CreateDialogue(c);
            d.Choose(i);
            Assert.True(d.IsEnded);
            Assert.Equal(expected[i], d.Action);
        }
    }

    [Fact]
    public void RestRestoresHealthAndEnergyAndAdvancesThreeHours()
    {
        var c = Hero();
        c.TakeDamage(c.MaxHealth / 2);
        c.SpendEnergy(c.MaxEnergy / 2);
        Assert.True(BarracksMaster.Apply(c, BarracksActions.Rest, BindPoint.Tutorial, out var notice));
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(c.MaxEnergy, c.Energy);
        Assert.Equal("11:00 am", c.Clock.TimeText);
        Assert.Equal(1, c.Clock.Day);
        Assert.NotNull(notice);
    }

    [Fact]
    public void RestCanRunIntoTheNextDay()
    {
        var c = Hero();
        c.Clock.Advance(14);   // 10 pm
        BarracksMaster.Apply(c, BarracksActions.Rest, BindPoint.Tutorial, out _);
        Assert.Equal(2, c.Clock.Day);
        Assert.Equal("1:00 am", c.Clock.TimeText);
    }

    [Fact]
    public void SleepBeforeMidnightWakesAtEightNextDay()
    {
        var c = Hero();
        c.Clock.Advance(14);   // 10 pm day 1
        c.TakeDamage(10);
        Assert.True(BarracksMaster.Apply(c, BarracksActions.Sleep, BindPoint.Tutorial, out var notice));
        Assert.Equal(2, c.Clock.Day);
        Assert.Equal("8:00 am", c.Clock.TimeText);
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Contains("Day 2, 8:00 am", notice);
    }

    [Fact]
    public void SleepAfterMidnightWakesAtEightSameDay()
    {
        var c = Hero();
        c.Clock.Advance(17);   // 1 am day 2
        Assert.Equal(2, c.Clock.Day);
        Assert.True(BarracksMaster.Apply(c, BarracksActions.Sleep, BindPoint.Tutorial, out _));
        Assert.Equal(2, c.Clock.Day);
        Assert.Equal("8:00 am", c.Clock.TimeText);
    }

    [Fact]
    public void SleepIsRefusedInTheDay()
    {
        var c = Hero();
        Assert.False(BarracksMaster.Apply(c, BarracksActions.Sleep, BindPoint.Tutorial, out _));
        Assert.Equal("8:00 am", c.Clock.TimeText);
    }

    [Fact]
    public void FallenCharacterCannotRestOrSleep()
    {
        var c = Hero();
        c.Clock.Advance(11);
        c.TakeDamage(c.MaxHealth * 2);
        Assert.True(c.IsDead);
        Assert.False(BarracksMaster.Apply(c, BarracksActions.Rest, BindPoint.Tutorial, out var n1));
        Assert.False(BarracksMaster.Apply(c, BarracksActions.Sleep, BindPoint.Tutorial, out var n2));
        Assert.NotNull(n1);
        Assert.NotNull(n2);
        Assert.True(c.IsDead);
        Assert.Equal("7:00 pm", c.Clock.TimeText);
    }

    [Fact]
    public void SetAsHomeChangesTheBindPointToATileNextToTheMaster()
    {
        var camp = Camp();
        var master = camp.Npcs[1];
        var home = BarracksMaster.HomePointFor(camp, master.X, master.Y);
        Assert.Equal(MapIds.AdventurersEncampment, home.MapId);
        Assert.True(camp.IsWalkable(home.TileX, home.TileY));
        Assert.True(TalkTarget.IsAdjacent(home.TileX, home.TileY, master.X, master.Y));
        var c = Hero();
        Assert.True(BarracksMaster.Apply(c, BarracksActions.SetAsHome, home, out var notice));
        Assert.Same(home, c.BindPoint);
        Assert.NotNull(notice);
    }

    [Fact]
    public void UnknownOrMissingActionDoesNothing()
    {
        var c = Hero();
        Assert.False(BarracksMaster.Apply(c, null, BindPoint.Tutorial, out var notice));
        Assert.Null(notice);
        Assert.False(BarracksMaster.Apply(c, "barracks:nope", BindPoint.Tutorial, out _));
        Assert.Equal("8:00 am", c.Clock.TimeText);
    }
}
