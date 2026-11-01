using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

public class CombatTests
{
    private static Character Make(StatDistribution d) =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = d }.Create();

    private static Character Warrior() => Make(StatDistribution.Warrior);   // Str 13, Con 13, Wis 7

    private static Character ArmedWarrior()
    {
        var c = Warrior();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static Character WarriorWithQuest()
    {
        var c = Warrior();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.True(ColonelPowersQuest.Accept(c, out _));
        return c;
    }

    private static Enemy SlimeAt(int x, int y)
    {
        var e = Enemy.NewSlime();
        e.TileX = x;
        e.TileY = y;
        return e;
    }

    private static List<Enemy> One(Enemy e) => new List<Enemy> { e };

    private static void Wait(Character c, double seconds)
    {
        for (double t = 0; t < seconds; t += 0.05) c.Combat.Update(0.05);
    }

    // ---- vitals (#12)

    [Fact]
    public void MaxHealthIs20TimesConAndMaxEnergyIs5TimesWisPerClass()
    {
        var w = Warrior();   // Con 13, Wis 7
        Assert.Equal(13, w.Stats.Constitution);
        Assert.Equal(7, w.Stats.Wisdom);
        Assert.Equal(260, w.MaxHealth);
        Assert.Equal(35, w.MaxEnergy);

        var me = Make(StatDistribution.Merchant);   // Con 9, Wis 9
        Assert.Equal(180, me.MaxHealth);
        Assert.Equal(45, me.MaxEnergy);

        var m = Make(StatDistribution.Mystic);   // Con 5, Wis 13
        Assert.Equal(100, m.MaxHealth);
        Assert.Equal(65, m.MaxEnergy);
    }

    [Fact]
    public void MaximaScaleLinearlyWithNoFloorOrMinimum()
    {
        var odd = new Character("Odd", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(7));
        Assert.Equal(140, odd.MaxHealth);   // 20 * 7, no rounding to blocks of 5
        Assert.Equal(35, odd.MaxEnergy);    // 5 * 7
        var low = new Character("Low", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(3));
        Assert.Equal(60, low.MaxHealth);    // no minimum
        Assert.Equal(15, low.MaxEnergy);
    }

    [Fact]
    public void HealthAndEnergyStartFullAndLevelStartsAtOne()
    {
        var c = Warrior();
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(c.MaxEnergy, c.Energy);
        Assert.Equal(1, c.Level);
    }

    [Fact]
    public void HealthAndEnergyStayWithinTheirBounds()
    {
        var c = Warrior();
        Assert.Equal(60, c.TakeDamage(60));
        Assert.Equal(200, c.Health);
        Assert.Equal(200, c.TakeDamage(1000));
        Assert.True(c.IsDead);
        c.Heal(10000);
        Assert.Equal(c.MaxHealth, c.Health);

        Assert.True(c.SpendEnergy(10));
        Assert.Equal(25, c.Energy);
        Assert.False(c.SpendEnergy(26));
        Assert.Equal(25, c.Energy);
        c.RegenEnergy(1000);
        Assert.Equal(c.MaxEnergy, c.Energy);
    }

    [Fact]
    public void EnergyRegeneratesTwoPerSecond()
    {
        var c = Warrior();
        c.SpendEnergy(30);
        Assert.Equal(5, c.Energy);
        Wait(c, 3.0);
        Assert.InRange(c.EnergyExact, 10.9, 11.1);
        Wait(c, 30);
        Assert.Equal(35, c.Energy);
    }

    // ---- damage formula (#11)

    [Fact]
    public void PracticeSwordDamageIsTenPlusHalfStrength()
    {
        var c = ArmedWarrior();   // Str 13 -> 10 + 6
        Assert.Equal(16, CombatController.BasicAttackDamage(c));
    }

    [Fact]
    public void UnarmedDamageIsTwoPlusQuarterStrength()
    {
        var c = Warrior();   // Str 13 -> 2 + 3
        Assert.Equal(5, CombatController.BasicAttackDamage(c));
        var weak = new Character("W", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(3));
        Assert.Equal(2, CombatController.BasicAttackDamage(weak));
    }

    [Fact]
    public void DamageDependsOnStrength()
    {
        var strong = new Character("S", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(20));
        strong.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        Assert.Equal(20, CombatController.BasicAttackDamage(strong));
    }

    [Fact]
    public void SlashIsOneAndAHalfTimesWeaponDamage()
    {
        var c = ArmedWarrior();
        Assert.Equal(24, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition));
        Assert.Equal(1.5, AbilityLibrary.SlashDefinition.DamageMultiplier);
        Assert.Equal(10, AbilityLibrary.SlashDefinition.EnergyCost);
        Assert.Equal(2.0, AbilityLibrary.SlashDefinition.CooldownSeconds);
    }

    // ---- basic attack

    [Fact]
    public void SlimesHaveThirtyHp()
    {
        var s = Enemy.NewSlime();
        Assert.Equal(30, s.MaxHp);
        Assert.Equal(30, s.Hp);
        Assert.False(s.IsDamaged);
    }

    [Fact]
    public void BasicAttackNeedsAnAdjacentLivingTarget()
    {
        var c = ArmedWarrior();
        Assert.Equal(CombatOutcome.NoTarget, c.Combat.BasicAttack(5, 5, new List<Enemy>()).Outcome);
        Assert.Equal(CombatOutcome.NoTarget, c.Combat.BasicAttack(5, 5, One(SlimeAt(7, 5))).Outcome);
        Assert.True(c.Combat.AttackReady);   // a miss costs no cooldown
        foreach (var tile in new[] { (6, 5), (4, 5), (5, 4), (5, 6), (6, 6), (4, 4), (6, 4), (4, 6), (5, 5) })
        {
            Wait(c, 1);
            Assert.True(c.Combat.BasicAttack(5, 5, One(SlimeAt(tile.Item1, tile.Item2))).Succeeded);
        }
    }

    [Fact]
    public void TheNearestStraightNeighbourIsHitFirst()
    {
        var c = ArmedWarrior();
        var diagonal = SlimeAt(6, 6);
        var straight = SlimeAt(6, 5);
        var r = c.Combat.BasicAttack(5, 5, new List<Enemy> { diagonal, straight });
        Assert.Same(straight, r.Target);
        Assert.Equal(30, diagonal.Hp);
    }

    [Fact]
    public void AHitDamagesFlickersAndTracksTheHitTime()
    {
        var c = ArmedWarrior();
        var s = SlimeAt(6, 5);
        s.Update(1.0);

        var r = c.Combat.BasicAttack(5, 5, One(s));

        Assert.Equal(CombatOutcome.Hit, r.Outcome);
        Assert.Equal(16, r.Damage);
        Assert.Equal(14, s.Hp);
        Assert.True(s.IsDamaged);
        Assert.Equal(1.0, s.LastHitTime);
        Assert.Equal(Enemy.FlashDuration, s.DamageFlash);
        s.Update(0.1);
        Assert.InRange(s.DamageFlash, 0.1, 0.2);
        s.Update(1);
        Assert.Equal(0, s.DamageFlash);
    }

    [Fact]
    public void AttackHasAHalfSecondCooldown()
    {
        var c = ArmedWarrior();
        var s = SlimeAt(6, 5);
        Assert.True(c.Combat.BasicAttack(5, 5, One(s)).Succeeded);
        Assert.Equal(CombatOutcome.OnCooldown, c.Combat.BasicAttack(5, 5, One(s)).Outcome);
        Assert.Equal(14, s.Hp);

        c.Combat.Update(0.3);
        Assert.Equal(CombatOutcome.OnCooldown, c.Combat.BasicAttack(5, 5, One(s)).Outcome);
        c.Combat.Update(0.25);
        Assert.Equal(CombatOutcome.Killed, c.Combat.BasicAttack(5, 5, One(s)).Outcome);
    }

    [Fact]
    public void UnarmedAttacksWorkButAreWeak()
    {
        var c = Warrior();
        var s = SlimeAt(6, 5);
        var r = c.Combat.BasicAttack(5, 5, One(s));
        Assert.Equal(5, r.Damage);
        Assert.Equal(25, s.Hp);
    }

    [Fact]
    public void DeadEnemiesAreNotTargetedAndTakeNoMoreDamage()
    {
        var c = ArmedWarrior();
        var s = SlimeAt(6, 5);
        s.TakeDamage(1000);
        Assert.True(s.IsDead);
        Assert.Equal(0, s.Hp);
        Assert.Equal(0, s.TakeDamage(5));
        Assert.Equal(CombatOutcome.NoTarget, c.Combat.BasicAttack(5, 5, One(s)).Outcome);
    }

    // ---- slime death counts toward the quest

    [Fact]
    public void KillingSlimesCountsTowardsTheQuestAndCompletesIt()
    {
        var c = WarriorWithQuest();
        c.Equipment.Get(EquipmentSlot.MainHand);   // the quest gave and equipped the sword
        var slimes = new List<Enemy>();
        for (int i = 0; i < 5; i++) slimes.Add(SlimeAt(6, 5));

        IReadOnlyList<Quest> completed = new Quest[0];
        for (int i = 0; i < 5; i++)
        {
            var s = slimes[i];
            Assert.Equal(CombatOutcome.Hit, c.Combat.BasicAttack(5, 5, slimes).Outcome);   // 16 of 30: not dead yet
            Assert.Equal($"Slimes slain {i}/5", c.Quests.Get(QuestLibrary.SliceAndSlimeId).ProgressText);
            Wait(c, 0.6);
            var kill = c.Combat.BasicAttack(5, 5, slimes);
            Assert.Equal(CombatOutcome.Killed, kill.Outcome);
            if (kill.CompletedQuests.Count > 0) completed = kill.CompletedQuests;
            Wait(c, 0.6);
        }

        Assert.Single(completed);
        Assert.Equal(QuestState.Complete, c.Quests.Get(QuestLibrary.SliceAndSlimeId).State);
        Assert.Equal(50, c.Xp);
        Assert.Equal(2, c.Level);          // the quest's 50 XP is exactly the cost of level 1 -> 2
        Assert.Equal(3, c.FreeStatPoints);
        Assert.Contains("slash", c.Abilities);
    }

    [Fact]
    public void ASlimeThatIsOnlyDamagedDoesNotCount()
    {
        var c = WarriorWithQuest();
        c.Combat.BasicAttack(5, 5, One(SlimeAt(6, 5)));
        Assert.Equal("Slimes slain 0/5", c.Quests.Get(QuestLibrary.SliceAndSlimeId).ProgressText);
    }

    // ---- hotbar and Slash

    [Fact]
    public void HotbarHasFiveEmptySlots()
    {
        var c = Warrior();
        Assert.Equal(5, c.Combat.Hotbar.Count);
        for (int i = 0; i < 5; i++) Assert.True(c.Combat.Hotbar[i].IsEmpty);
        Assert.Equal(CombatOutcome.EmptySlot, c.Combat.UseHotbar(0, 5, 5, One(SlimeAt(6, 5))).Outcome);
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Combat.Hotbar.Get(5));
    }

    [Fact]
    public void SlashLandsInSlotOneOnceLearned()
    {
        var c = Warrior();
        c.LearnAbility(AbilityLibrary.Slash);
        c.Combat.Update(0.016);
        Assert.Equal("slash", c.Combat.Hotbar[0].AbilityId);
        Assert.True(c.Combat.Hotbar[1].IsEmpty);
        c.Combat.Update(0.016);
        Assert.Equal(0, c.Combat.Hotbar.IndexOf("slash"));
    }

    [Fact]
    public void FinishingTheQuestPutsSlashOnTheHotbar()
    {
        var c = WarriorWithQuest();
        var slimes = new List<Enemy>();
        for (int i = 0; i < 5; i++) slimes.Add(SlimeAt(6, 5));
        foreach (var s in slimes) s.TakeDamage(1000);
        c.Quests.RecordKill("slime"); c.Quests.RecordKill("slime"); c.Quests.RecordKill("slime"); c.Quests.RecordKill("slime"); c.Quests.RecordKill("slime");
        c.Combat.Update(0.016);
        Assert.Equal("slash", c.Combat.Hotbar[0].AbilityId);
    }

    [Fact]
    public void SlashDealsOneAndAHalfDamageCostsEnergyAndStartsACooldown()
    {
        var c = ArmedWarrior();
        c.LearnAbility(AbilityLibrary.Slash);
        c.Combat.Update(0.016);
        var s = SlimeAt(6, 5);

        var r = c.Combat.UseHotbar(0, 5, 5, One(s));

        Assert.Equal(CombatOutcome.Hit, r.Outcome);
        Assert.Equal(24, r.Damage);
        Assert.Equal(6, s.Hp);
        Assert.Equal(25, c.Energy);
        var slot = c.Combat.Hotbar[0];
        Assert.False(slot.IsReady);
        Assert.Equal(2.0, slot.CooldownRemaining, 2);
        Assert.Equal(1.0, slot.CooldownProgress, 2);

        c.Combat.Update(1.0);
        Assert.InRange(slot.CooldownProgress, 0.45, 0.55);
        c.Combat.Update(1.0);
        Assert.Equal(0, slot.CooldownProgress);
        Assert.True(slot.IsReady);
    }

    [Fact]
    public void SlashCannotBeUsedOnCooldownOrWithoutEnoughEnergy()
    {
        var c = ArmedWarrior();   // 35 energy
        c.LearnAbility(AbilityLibrary.Slash);
        var enemies = One(SlimeAt(6, 5));
        enemies[0].TakeDamage(0);

        Assert.True(c.Combat.UseHotbar(0, 5, 5, enemies).Succeeded);
        Wait(c, 0.6);
        Assert.Equal(CombatOutcome.OnCooldown, c.Combat.UseHotbar(0, 5, 5, enemies).Outcome);
        Assert.InRange(c.Energy, 25, 27);

        var far = new List<Enemy> { SlimeAt(9, 9) };
        Wait(c, 2.5);
        Assert.Equal(CombatOutcome.NoTarget, c.Combat.UseHotbar(0, 5, 5, far).Outcome);   // no target: no cost

        var rich = ArmedWarrior();
        rich.LearnAbility(AbilityLibrary.Slash);
        rich.SpendEnergy(30);   // 5 left
        Assert.Equal(CombatOutcome.NotEnoughEnergy, rich.Combat.UseHotbar(0, 5, 5, One(SlimeAt(6, 5))).Outcome);
        Assert.Equal(5, rich.Energy);
    }

    [Fact]
    public void SlashStillWorksWhenBasicAttackAlsoShareTheHalfSecondDelay()
    {
        var c = ArmedWarrior();
        c.LearnAbility(AbilityLibrary.Slash);
        var s = SlimeAt(6, 5);
        Assert.True(c.Combat.BasicAttack(5, 5, One(s)).Succeeded);
        Assert.Equal(CombatOutcome.OnCooldown, c.Combat.UseHotbar(0, 5, 5, One(s)).Outcome);
        c.Combat.Update(0.5);
        Assert.Equal(CombatOutcome.Killed, c.Combat.UseHotbar(0, 5, 5, One(s)).Outcome);   // 14 hp left, 24 damage
    }
}
