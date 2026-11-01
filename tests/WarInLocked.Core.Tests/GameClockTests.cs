using WarInLocked.Core.Characters;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#56 game clock.</summary>
public class GameClockTests
{
    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    [Fact]
    public void StartsAtEightAmOnDayOne()
    {
        var c = new GameClock();
        Assert.Equal(1, c.Day);
        Assert.Equal(8, c.Hour);
        Assert.Equal(0, c.Minute);
        Assert.Equal("8:00 am", c.TimeText);
        Assert.Equal("Day 1, 8:00 am", c.Text);
        Assert.False(c.IsNight);
    }

    [Fact]
    public void RunsFiveTimesFasterThanRealTime()
    {
        var c = new GameClock();
        c.Update(60);                       // one real minute = five game minutes
        Assert.Equal(8, c.Hour);
        Assert.Equal(5, c.Minute);
        c.Update(12 * 60);                  // twelve real minutes = one game hour
        Assert.Equal(9, c.Hour);
        Assert.Equal(5, c.Minute);
        Assert.Equal("9:05 am", c.TimeText);
    }

    [Fact]
    public void ADayIsFourPointEightRealHours()
    {
        var c = new GameClock();
        c.Update(288 * 60);
        Assert.Equal(2, c.Day);
        Assert.Equal(8, c.Hour);
        Assert.Equal(0, c.Minute);
    }

    [Fact]
    public void AdvanceMovesByGameHoursAndRollsTheDay()
    {
        var c = new GameClock();
        c.Advance(3);
        Assert.Equal("11:00 am", c.TimeText);
        c.Advance(1);
        Assert.Equal("12:00 pm", c.TimeText);
        c.Advance(12);
        Assert.Equal("12:00 am", c.TimeText);
        Assert.Equal(2, c.Day);
        c.Advance(1.5);
        Assert.Equal("1:30 am", c.TimeText);
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Update(-1));
    }

    [Theory]
    [InlineData(0, false)]   // placeholder gap hours count as day
    [InlineData(6, true)]
    [InlineData(7, false)]   // night ends at 7 am
    [InlineData(8, false)]
    [InlineData(12, false)]
    [InlineData(18, false)]  // 6-7 pm: undefined, day (placeholder)
    [InlineData(19, true)]
    [InlineData(23, true)]
    public void NightIsSevenPmToSevenAm(int hour, bool night)
    {
        if (hour == 0) { Assert.True(new GameClock().IsDay); return; }   // start is 8 am: day
        var c = new GameClock();
        c.Advance((hour - 8 + 24) % 24);
        Assert.Equal(hour, c.Hour);
        Assert.Equal(night, c.IsNight);
        Assert.Equal(!night, c.IsDay);
    }

    [Fact]
    public void MidnightIsNight()
    {
        var c = new GameClock();
        c.Advance(16);
        Assert.Equal(0, c.Hour);
        Assert.True(c.IsNight);
        Assert.Equal("12:00 am", c.TimeText);
    }

    [Fact]
    public void AdvanceToMorningWakesAtEightAmHandlingMidnight()
    {
        var evening = new GameClock();
        evening.Advance(12);                                   // 8 pm day 1
        Assert.Equal(12.0, evening.AdvanceToMorning(), 6);
        Assert.Equal(2, evening.Day);
        Assert.Equal("8:00 am", evening.TimeText);

        var afterMidnight = new GameClock();
        afterMidnight.Advance(19);                             // 3 am day 2
        Assert.Equal(2, afterMidnight.Day);
        Assert.Equal(5.0, afterMidnight.AdvanceToMorning(), 6);
        Assert.Equal(2, afterMidnight.Day);                    // same day, not day 3
        Assert.Equal("8:00 am", afterMidnight.TimeText);

        var exact = new GameClock();
        Assert.Equal(24.0, exact.AdvanceToMorning(), 6);       // exactly 8 am goes to tomorrow
        Assert.Equal(2, exact.Day);

        var late = new GameClock();
        late.Advance(15.99);                                   // 11:59 pm
        Assert.Equal(1, late.Day);
        late.AdvanceToMorning();
        Assert.Equal(2, late.Day);
        Assert.Equal(8, late.Hour);
    }

    [Fact]
    public void CharacterOwnsTheClockAndCombatUpdateTicksIt()
    {
        var c = Hero();
        Assert.Equal("Day 1, 8:00 am", c.Clock.Text);
        c.Combat.Update(60);
        Assert.Equal("8:05 am", c.Clock.TimeText);
        c.Combat.Update(60);
        Assert.Equal("8:10 am", c.Clock.TimeText);
    }

    [Fact]
    public void ClockStandsStillWhenNoOneCallsUpdate()
    {
        var c = Hero();
        double before = c.Clock.TotalMinutes;
        int v = c.Clock.Version;
        // a menu is open: the Game does not call Combat.Update
        Assert.Equal(before, c.Clock.TotalMinutes);
        Assert.Equal(v, c.Clock.Version);
        c.Combat.Update(0);
        Assert.Equal(before, c.Clock.TotalMinutes);
    }

    [Fact]
    public void ChangedAndVersionFireWhenTheTimeMoves()
    {
        var c = new GameClock();
        int n = 0;
        c.Changed += () => n++;
        c.Update(1);
        c.Advance(1);
        c.SetTotalMinutes(100);
        Assert.Equal(3, n);
        Assert.Equal(3, c.Version);
        Assert.Equal(1, c.Day);
        Assert.Equal(1, c.Hour);
    }
}
