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

    /// <summary>Warrior vitals (Con 13, Wis 7: 260 HP, 35 energy) but Strength 8 (modifier 0.8), so fights last several hits:
    /// sword (10 + 5) x 0.8 = 12, unarmed 8, Slash 18, slime 30 HP.</summary>
    private static Character Rookie() =>
        new Character("Rookie", Race.Human, StatDistribution.Warrior, new StatBlock(8, 8, 13, 7, 6, 10, 8, 8));

    private static Character ArmedWarrior()
    {
        var c = Rookie();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static Character WarriorWithQuest()
    {
        var c = Rookie();
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
    public void MaxHealthIs100TimesTheConModifierAndMaxEnergyIs50TimesTheWisModifierPerClass()
    {
        var w = Warrior();   // Con 13, Wis 7
        Assert.Equal(13, w.Stats.Constitution);
        Assert.Equal(7, w.Stats.Wisdom);
        Assert.Equal(230, w.MaxHealth);   // 100 x 2.3
        Assert.Equal(35, w.MaxEnergy);

        var me = Make(StatDistribution.Merchant);   // Con 9, Wis 9
        Assert.Equal(90, me.MaxHealth);   // 100 x 0.9
        Assert.Equal(45, me.MaxEnergy);

        var m = Make(StatDistribution.Mystic);   // Con 5, Wis 13
        Assert.Equal(50, m.MaxHealth);   // 100 x 0.5
        Assert.Equal(115, m.MaxEnergy);   // 50 x 2.3
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 30)]
    [InlineData(5, 50)]
    [InlineData(9, 90)]
    [InlineData(10, 200)]    // first milestone: 100 x 2.0
    [InlineData(11, 210)]
    [InlineData(13, 230)]
    [InlineData(24, 340)]
    [InlineData(25, 450)]    // 100 x 4.5
    [InlineData(50, 800)]    // 100 x 8.0
    public void MaxHealthFollowsTheConModifierCurve(int con, int expected)
    {
        var c = new Character("C", Race.Human, StatDistribution.Warrior, new StatBlock(8, 8, con, 8, 8, 8, 8, 8));
        Assert.Equal(expected, c.MaxHealth);
        Assert.Equal(expected, c.Health);   // starts full
        Assert.Equal(Character.HealthBase, 100);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 15)]
    [InlineData(7, 35)]
    [InlineData(9, 45)]
    [InlineData(10, 100)]    // first milestone: 50 x 2.0
    [InlineData(13, 115)]
    [InlineData(25, 225)]    // 50 x 4.5
    [InlineData(50, 400)]    // 50 x 8.0
    public void MaxEnergyFollowsTheWisModifierCurve(int wis, int expected)
    {
        var c = new Character("W", Race.Human, StatDistribution.Warrior, new StatBlock(8, 8, 8, wis, 8, 8, 8, 8));
        Assert.Equal(expected, c.MaxEnergy);
        Assert.Equal(expected, c.Energy);   // starts full
        Assert.Equal(50, Character.EnergyBase);
    }

    [Fact]
    public void EveryClassCanAffordSlashFromFullEnergy()
    {
        foreach (var d in new[] { StatDistribution.Warrior, StatDistribution.Merchant, StatDistribution.Mystic })
            Assert.True(Make(d).MaxEnergy >= AbilityLibrary.SlashDefinition.EnergyCost);
    }

    [Fact]
    public void ModifierReturnsTheStatModifierOfTheCurrentValue()
    {
        var c = new Character("M", Race.Human, StatDistribution.Warrior, new StatBlock(7, 9, 13, 10, 25, 50, 11, 8));
        Assert.Equal(0.7, c.Modifier(StatType.Strength), 5);
        Assert.Equal(0.9, c.Modifier(StatType.Dexterity), 5);
        Assert.Equal(2.3, c.Modifier(StatType.Constitution), 5);
        Assert.Equal(2.0, c.Modifier(StatType.Wisdom), 5);
        Assert.Equal(4.5, c.Modifier(StatType.Intelligence), 5);
        Assert.Equal(8.0, c.Modifier(StatType.Perception), 5);
        Assert.Equal(2.1, c.Modifier(StatType.Charisma), 5);
        Assert.Equal(0.8, c.Modifier(StatType.Luck), 5);
    }

    [Fact]
    public void ModifierTracksAssignedPoints()
    {
        var c = Make(StatDistribution.Merchant);   // Wis 9
        c.GainXp(50);
        Assert.Equal(0.9, c.Modifier(StatType.Wisdom), 5);
        c.AssignPoint(StatType.Wisdom);
        Assert.Equal(2.0, c.Modifier(StatType.Wisdom), 5);
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
        Assert.Equal(170, c.Health);
        Assert.Equal(170, c.TakeDamage(1000));
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
    public void PracticeSwordDamageIsFifteenTimesTheStrengthModifier()
    {
        Assert.Equal(35, CombatController.BasicAttackDamage(ArmedStarterWarrior()));   // Str 13 -> 2.3 x 15 = 34.5 -> 35
        Assert.Equal(12, CombatController.BasicAttackDamage(ArmedWarrior()));          // Str 8  -> 0.8 x 15
    }

    [Fact]
    public void UnarmedDamageIsTenTimesTheStrengthModifier()
    {
        Assert.Equal(23, CombatController.BasicAttackDamage(Warrior()));   // Str 13 -> 2.3 x 10
        Assert.Equal(8, CombatController.BasicAttackDamage(Rookie()));
        var weak = new Character("W", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(3));
        Assert.Equal(3, CombatController.BasicAttackDamage(weak));         // 0.3 x 10
    }

    [Fact]
    public void DamageDependsOnStrength()
    {
        var strong = new Character("S", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(25));
        strong.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        Assert.Equal(68, CombatController.BasicAttackDamage(strong));      // 4.5 x 15 = 67.5 -> 68
    }

    private static Character ArmedStarterWarrior()
    {
        var c = Warrior();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    [Fact]
    public void SlashIsOneAndAHalfTimesWeaponDamage()
    {
        Assert.Equal(52, CombatController.AbilityDamage(ArmedStarterWarrior(), AbilityLibrary.SlashDefinition));   // 15 x 2.3 x 1.5 = 51.75, rounded once at the end
        var c = ArmedWarrior();
        Assert.Equal(18, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition));
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
        Assert.Equal(12, r.Damage);
        Assert.Equal(18, s.Hp);
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
        Assert.Equal(18, s.Hp);

        c.Combat.Update(0.3);
        Assert.Equal(CombatOutcome.OnCooldown, c.Combat.BasicAttack(5, 5, One(s)).Outcome);
        c.Combat.Update(0.25);
        Assert.Equal(CombatOutcome.Hit, c.Combat.BasicAttack(5, 5, One(s)).Outcome);   // 6 left
        c.Combat.Update(0.5);
        Assert.Equal(CombatOutcome.Killed, c.Combat.BasicAttack(5, 5, One(s)).Outcome);
    }

    [Fact]
    public void UnarmedAttacksWorkButAreWeak()
    {
        var c = Rookie();
        var s = SlimeAt(6, 5);
        var r = c.Combat.BasicAttack(5, 5, One(s));
        Assert.Equal(8, r.Damage);
        Assert.Equal(22, s.Hp);
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
            Assert.Equal(CombatOutcome.Hit, c.Combat.BasicAttack(5, 5, slimes).Outcome);   // 12 of 30: not dead yet
            Assert.Equal($"Slimes slain {i}/5", c.Quests.Get(QuestLibrary.SliceAndSlimeId).ProgressText);
            Wait(c, 0.6);
            Assert.Equal(CombatOutcome.Hit, c.Combat.BasicAttack(5, 5, slimes).Outcome);   // 24 of 30
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
        Assert.Equal(18, r.Damage);
        Assert.Equal(12, s.Hp);
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
        Assert.Equal(CombatOutcome.Killed, c.Combat.UseHotbar(0, 5, 5, One(s)).Outcome);   // 18 hp left, 18 damage
    }
}
