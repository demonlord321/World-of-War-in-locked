using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Saving;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#35 stacking Death debuff.</summary>
public class DeathDebuffTests
{
    private static Character Hero()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static void Die(Character c)
    {
        c.TakeDamage(c.MaxHealth + 10);
        c.UpdateRespawn(Character.RespawnDelaySeconds);
    }

    [Fact]
    public void NoDebuffToStartWith()
    {
        var c = Hero();
        Assert.Equal(0, c.DeathDebuffLevel);
        Assert.False(c.HasDeathDebuff);
        Assert.Equal(1.0, c.DeathDebuffMultiplier);
        Assert.Null(c.DeathDebuffName);
        Assert.Null(c.DeathDebuffText);
        Assert.True(c.EarnsKillXp);
        Assert.Equal(230, c.MaxHealth);
    }

    [Fact]
    public void FirstRespawnGivesLevelOneForThirtySeconds()
    {
        var c = Hero();
        int changed = 0;
        c.DeathDebuffChanged += () => changed++;
        Die(c);
        Assert.Equal(1, c.DeathDebuffLevel);
        Assert.Equal(30.0, c.DeathDebuffRemaining);
        Assert.Equal(0.75, c.DeathDebuffMultiplier);
        Assert.Equal("Death debuff 1", c.DeathDebuffName);
        Assert.Equal("Death debuff 1: -25% health, attack and energy (0:30)", c.DeathDebuffText);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void TheCutAppliesToMaxHealthAndEnergyAndRespawnFillsToTheReducedMax()
    {
        var c = Hero();                       // 230 HP, 35 energy
        Die(c);
        Assert.Equal(173, c.MaxHealth);       // 230 x 0.75 = 172.5 -> 173
        Assert.Equal(26, c.MaxEnergy);        // 35 x 0.75 = 26.25 -> 26
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(c.MaxEnergy, c.Energy);
    }

    [Fact]
    public void AttackPowerIsCutToo()
    {
        var c = Hero();
        // Warrior Strength 13 -> modifier 2.3, Practice Sword: (10 + 5) x 2.3 = 34.5 -> 35; the cut is applied before the one rounding.
        Assert.Equal(35, CombatController.BasicAttackDamage(c));
        Die(c);
        Assert.Equal(26, CombatController.BasicAttackDamage(c));   // 25.875
        Die(c);
        Assert.Equal(17, CombatController.BasicAttackDamage(c));   // 17.25
        Die(c);
        Assert.Equal(9, CombatController.BasicAttackDamage(c));    // 8.625
        Assert.Equal(DamageCalculator.UnarmedDamage(c) > 0, true);
    }

    [Fact]
    public void DyingWhileActiveStacksToTwoThenThreeAndStaysAtThree()
    {
        var c = Hero();
        Die(c);
        c.Combat.Update(10);
        Die(c);
        Assert.Equal(2, c.DeathDebuffLevel);
        Assert.Equal(60.0, c.DeathDebuffRemaining);
        Assert.Equal(0.5, c.DeathDebuffMultiplier);
        Assert.Equal("Death debuff 2: -50% health, attack and energy (1:00)", c.DeathDebuffText);
        Assert.Equal(115, c.MaxHealth);
        c.Combat.Update(18);
        Assert.Equal("Death debuff 2: -50% health, attack and energy (0:42)", c.DeathDebuffText);
        Die(c);
        Assert.Equal(3, c.DeathDebuffLevel);
        Assert.Equal(60.0, c.DeathDebuffRemaining);
        Assert.Equal(0.25, c.DeathDebuffMultiplier);
        Assert.Equal("Death debuff 3: -75% health, attack and energy, no XP from kills (1:00)", c.DeathDebuffText);
        Assert.Equal(58, c.MaxHealth);        // 57.5 -> 58
        c.Combat.Update(25);
        Die(c);
        Assert.Equal(3, c.DeathDebuffLevel);                 // level 3 is the maximum
        Assert.Equal(60.0, c.DeathDebuffRemaining);          // guess: the timer restarts
    }

    [Fact]
    public void WhenTheTimerRunsOutTheWholeStackGoesAndMaxesComeBack()
    {
        var c = Hero();
        Die(c); Die(c);
        Assert.Equal(2, c.DeathDebuffLevel);
        int changed = 0;
        c.DeathDebuffChanged += () => changed++;
        c.Combat.Update(59.9);
        Assert.Equal(2, c.DeathDebuffLevel);
        c.Combat.Update(0.2);
        Assert.Equal(0, c.DeathDebuffLevel);
        Assert.Equal(0.0, c.DeathDebuffRemaining);
        Assert.Equal(230, c.MaxHealth);
        Assert.Equal(1, changed);
        Assert.Null(c.DeathDebuffText);
        Die(c);                                               // the next death starts again at level 1
        Assert.Equal(1, c.DeathDebuffLevel);
    }

    [Fact]
    public void ExpiryDoesNotRefillHealthButRegenerationCan()
    {
        var c = Hero();
        Die(c);
        c.Combat.Update(30.1);
        Assert.Equal(173, c.Health);                          // guess: the lost part is not handed back
        Assert.Equal(230, c.MaxHealth);
        c.Heal(1000);
        Assert.Equal(230, c.Health);
    }

    [Fact]
    public void TheTimerRunsOutOfCombatAndIsFrozenWhileFallen()
    {
        var c = Hero();
        Die(c);
        Assert.False(c.InCombat);
        c.Combat.Update(12);
        Assert.Equal(18.0, c.DeathDebuffRemaining, 6);
        c.TakeDamage(c.MaxHealth + 10);
        c.Combat.Update(4);                                   // fallen: respawn is not yet due (5 s) and the debuff does not tick
        Assert.True(c.IsDead);
        Assert.Equal(18.0, c.DeathDebuffRemaining, 6);
        c.Combat.Update(1.1);
        Assert.False(c.IsDead);
        Assert.Equal(2, c.DeathDebuffLevel);
    }

    [Fact]
    public void ItStacksWithTheXpDeathPenalty()
    {
        var c = Hero();
        c.GainXp(60);                                         // level 2 at 50 XP: 10 XP into the level
        int xp = c.Xp;
        Die(c);
        Assert.True(c.Xp < xp || xp == (int)Leveling.TotalXpFor(c.Level));
        Assert.Equal(1, c.DeathDebuffLevel);
        Assert.Equal(xp - c.LastRespawnXpLost, c.Xp);
    }

    [Fact]
    public void LevelThreeEarnsNoKillXpButQuestXpStillCounts()
    {
        var c = Hero();
        Die(c); Die(c); Die(c);
        Assert.False(c.EarnsKillXp);
        var slime = Enemy.NewSlime(1); slime.TileX = 6; slime.TileY = 5;
        CombatResult r = null;
        for (int i = 0; i < 400 && (r == null || r.Outcome != CombatOutcome.Killed); i++)
        {
            c.Combat.Update(0.6);
            r = c.Combat.BasicAttack(5, 5, new[] { slime });
        }
        Assert.Equal(CombatOutcome.Killed, r.Outcome);
        Assert.Equal(0, r.XpGained);
        Assert.True(r.XpBlocked);
        Assert.Equal(0, c.Xp);
        Assert.NotNull(r.DroppedItem);                        // the jelly still drops (on the ground)
        Assert.Equal(1, c.Combat.Ground.Count);
        int before = c.Xp;
        c.GainXp(50);                                         // quest rewards go through GainXp and are not blocked
        Assert.Equal(before + 50, c.Xp);
    }

    [Fact]
    public void LevelTwoStillEarnsKillXp()
    {
        var c = Hero();
        Die(c); Die(c);
        Assert.True(c.EarnsKillXp);
        var slime = Enemy.NewSlime(1); slime.TileX = 6; slime.TileY = 5;
        CombatResult r = null;
        for (int i = 0; i < 400 && (r == null || r.Outcome != CombatOutcome.Killed); i++)
        {
            c.Combat.Update(0.6);
            r = c.Combat.BasicAttack(5, 5, new[] { slime });
        }
        Assert.Equal(5, r.XpGained);
        Assert.False(r.XpBlocked);
    }

    [Fact]
    public void ItIsSavedAndRestored()
    {
        var c = Hero();
        Die(c); Die(c);
        c.Combat.Update(20);
        var back = SaveGame.Capture(c, MapIds.Forest, 24, 13).ToCharacter();
        Assert.Equal(2, back.DeathDebuffLevel);
        Assert.Equal(40.0, back.DeathDebuffRemaining, 6);
        Assert.Equal(c.MaxHealth, back.MaxHealth);
        Assert.Equal(c.Health, back.Health);
        var json = SaveGame.Capture(c, MapIds.Forest, 24, 13).ToJson();
        Assert.Equal(2, SaveGame.FromJson(json).ToCharacter().DeathDebuffLevel);
    }

    [Fact]
    public void OlderSavesWithoutTheFieldLoadWithNoDebuff()
    {
        var c = Hero();
        var s = SaveGame.Capture(c, MapIds.Forest, 24, 13);
        Assert.Equal(0, s.DeathDebuffLevel);
        Assert.Equal(0, s.ToCharacter().DeathDebuffLevel);
    }

    [Theory]
    [InlineData(0, 100, 0, 0)]
    [InlineData(1, 75, 25, 30)]
    [InlineData(2, 50, 50, 60)]
    [InlineData(3, 25, 75, 60)]
    [InlineData(7, 25, 75, 60)]
    public void Numbers(int level, int multiplierPercent, int cutPercent, int seconds)
    {
        Assert.Equal(multiplierPercent, (int)System.Math.Round(DeathDebuff.MultiplierFor(level) * 100));
        Assert.Equal(cutPercent, DeathDebuff.PercentFor(level));
        Assert.Equal(seconds, DeathDebuff.SecondsFor(level > 3 ? 3 : level));
    }

    [Fact]
    public void TextRoundsRemainingTimeUpToWholeSeconds()
    {
        Assert.Equal("Death debuff 1: -25% health, attack and energy (0:30)", DeathDebuff.TextFor(1, 29.2));
        Assert.Equal("Death debuff 1: -25% health, attack and energy (0:01)", DeathDebuff.TextFor(1, 0.1));
        Assert.Equal("Death debuff 2: -50% health, attack and energy (1:00)", DeathDebuff.TextFor(2, 60));
        Assert.Null(DeathDebuff.TextFor(0, 10));
    }
}
