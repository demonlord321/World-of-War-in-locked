using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

/// <summary>Ability levels, uses, the weapon requirement and Energy Ball (#19, #20).</summary>
public class AbilityTests
{
    // Str 8 / Int 13 so a slime survives a hit; plenty of energy for repeated casts.
    private static Character Make(bool armed, bool energyBall = true, bool staff = false)
    {
        var c = new Character("Tester", Race.Human, StatDistribution.Warrior, new StatBlock(8, 8, 13, 13, 13, 10, 8, 8));
        if (armed) c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        if (staff) c.Equipment.AddAndEquip(ItemLibrary.PracticeStaff);
        c.LearnAbility(AbilityLibrary.Slash);
        if (energyBall) c.LearnAbility(AbilityLibrary.EnergyBall);
        c.Combat.Update(0.016);
        return c;
    }

    private static Enemy Dummy(int hp = 100000) => new Enemy("dummy", hp) { TileX = 6, TileY = 5 };

    private static void Wait(Character c, double seconds)
    {
        for (double t = 0; t < seconds; t += 0.1) c.Combat.Update(0.1);
    }

    private static CombatResult CastSlash(Character c, Enemy e)
    {
        c.RegenEnergy(1000);
        var r = c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { e });
        Wait(c, 5.1);
        return r;
    }

    // ---- levels and uses

    [Fact]
    public void ALearnedAbilityStartsAtLevelOneWithNoUses()
    {
        var c = Make(true);
        var a = c.GetAbility("slash");
        Assert.Equal(1, a.Level);
        Assert.Equal(0, a.Uses);
        Assert.Equal(0, a.UsesIntoLevel);
        Assert.Equal(5, a.UsesForNextLevel);
        Assert.Equal(0.0, a.Fraction);
        Assert.Equal("Slash", a.Name);
        Assert.Equal("Cut an arc with your blade.", a.Description);
        Assert.Equal(5, a.Power);
        Assert.Equal(2, c.LearnedAbilities.Count);
        Assert.Null(c.GetAbility("nope"));
    }

    [Fact]
    public void UsesNeededDoubleEachLevelAndCountWithinTheLevel()
    {
        Assert.Equal(5, Ability.UsesForLevel(1));
        Assert.Equal(10, Ability.UsesForLevel(2));
        Assert.Equal(20, Ability.UsesForLevel(3));
        Assert.Equal(40, Ability.UsesForLevel(4));

        var a = new Ability(AbilityLibrary.SlashDefinition);
        var raised = new List<int>();
        a.LeveledUp += x => raised.Add(x.Level);
        int v = a.Version;

        for (int i = 0; i < 4; i++) Assert.False(a.RecordUse());
        Assert.Equal(1, a.Level);
        Assert.Equal(0.8, a.Fraction, 5);
        Assert.Equal(v + 4, a.Version);

        Assert.True(a.RecordUse());                 // 5th use: level 2
        Assert.Equal(2, a.Level);
        Assert.Equal(5, a.Uses);
        Assert.Equal(0, a.UsesIntoLevel);           // the counter restarts
        Assert.Equal(10, a.UsesForNextLevel);
        Assert.Equal(6, a.Power);

        for (int i = 0; i < 9; i++) Assert.False(a.RecordUse());
        Assert.Equal(2, a.Level);
        Assert.Equal(0.9, a.Fraction, 5);
        Assert.True(a.RecordUse());                 // 10 more: level 3 after 15 total
        Assert.Equal(3, a.Level);
        Assert.Equal(15, a.Uses);
        Assert.Equal(20, a.UsesForNextLevel);
        Assert.Equal(7, a.Power);
        Assert.Equal(new[] { 2, 3 }, raised);
    }

    [Fact]
    public void EveryCastThatFiresAddsAUseAndTheFifthLevelsSlash()
    {
        var c = Make(true);
        var dummy = Dummy();
        int leveledEvents = 0;
        c.AbilityLeveledUp += _ => leveledEvents++;

        CombatResult last = null;
        for (int i = 0; i < 5; i++) last = CastSlash(c, dummy);

        var slash = c.GetAbility("slash");
        Assert.Equal(2, slash.Level);
        Assert.Equal(5, slash.Uses);
        Assert.True(last.AbilityLeveledUp);
        Assert.Same(slash, last.AbilityUsed);
        Assert.Equal(1, leveledEvents);
        Assert.Equal(17, CombatController.AbilityDamage(c, slash));
        Assert.Equal(CombatController.AbilityDamage(c, slash), CastSlash(c, dummy).Damage);   // the cast uses the new level
    }

    [Fact]
    public void FailedCastsDoNotCount()
    {
        var c = Make(true);
        var slash = c.GetAbility("slash");

        // no target
        Assert.Equal(CombatOutcome.NoTarget, c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { new Enemy("d", 10) { TileX = 20, TileY = 20 } }).Outcome);
        Assert.Equal(0, slash.Uses);

        // on cooldown
        Assert.True(c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() }).Succeeded);
        Assert.Equal(1, slash.Uses);
        Wait(c, 0.6);
        Assert.Equal(CombatOutcome.OnCooldown, c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() }).Outcome);
        Assert.Equal(1, slash.Uses);

        // not enough energy
        Wait(c, 5.0);
        c.SpendEnergy(c.Energy - 5);
        Assert.Equal(CombatOutcome.NotEnoughEnergy, c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() }).Outcome);
        Assert.Equal(1, slash.Uses);

        // basic attacks never count
        Wait(c, 1.0);
        c.Combat.BasicAttack(5, 5, new List<Enemy> { Dummy() });
        Assert.Equal(1, slash.Uses);
    }

    // ---- Slash needs a weapon

    [Fact]
    public void SlashCannotBeCastWithoutAWeaponAndCostsNothing()
    {
        var c = Make(false);
        var slash = c.GetAbility("slash");
        int energy = c.Energy;
        var dummy = Dummy();

        var r = c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { dummy });

        Assert.Equal(CombatOutcome.NeedsWeapon, r.Outcome);
        Assert.False(r.Succeeded);
        Assert.Equal(energy, c.Energy);
        Assert.Equal(0, slash.Uses);
        Assert.Equal(dummy.MaxHp, dummy.Hp);
        Assert.True(c.Combat.Hotbar[0].IsReady);          // no cooldown started
        Assert.True(c.Combat.AttackReady);                // and no attack delay either
        Assert.Equal("Needs a weapon", CombatController.CannotCastReason(c, AbilityLibrary.SlashDefinition));
        Assert.Empty(c.Quests.RecordAbilityUse("nothing"));
    }

    [Fact]
    public void SlashWorksOnceAWeaponIsEquippedAndStopsWhenItIsRemoved()
    {
        var c = Make(false);
        Assert.Equal(CombatOutcome.NeedsWeapon, c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() }).Outcome);
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        Assert.Null(CombatController.CannotCastReason(c, AbilityLibrary.SlashDefinition));
        Assert.True(c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() }).Succeeded);

        c.Equipment.Unequip(EquipmentSlot.MainHand);
        Wait(c, 5.1);
        Assert.Equal(CombatOutcome.NeedsWeapon, c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() }).Outcome);
        Assert.Equal(1, c.GetAbility("slash").Uses);
    }

    [Fact]
    public void UnarmedSlashDoesNotCountForTheQuest()
    {
        var c = Make(false);
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);                       // gives and equips the sword
        c.Equipment.Unequip(EquipmentSlot.MainHand);
        for (int i = 0; i < 5; i++) c.Combat.UseHotbar(0, 5, 5, new List<Enemy> { Dummy() });
        Assert.Equal(0, c.GetAbility("slash").Uses);
    }

    [Fact]
    public void SlashDamageIncludesTheWeaponBonus()
    {
        var armed = Make(true);       // (10 + 5 + 5) x 0.8 = 16
        var d = Dummy();
        Assert.Equal(16, armed.Combat.UseHotbar(0, 5, 5, new List<Enemy> { d }).Damage);
        Assert.Equal(16, d.MaxHp - d.Hp);
    }

    // ---- quest 2 counts casts that hit an enemy

    [Fact]
    public void FiveSlashesThatHitMakeTheQuestReadyAndTurnInGivesEnergyBall()
    {
        var c = Make(true, energyBall: false);
        // Put the quest chain in the state "quest 2 active".
        var q1 = c.Quests.Offer(QuestLibrary.SliceAndSlime());
        c.Quests.Accept(q1.Id);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.True(ColonelPowersQuest.Accept(c, out _));
        var q2 = c.Quests.Get(QuestLibrary.SlashingPracticeId);

        var dummy = Dummy();
        CombatResult r = null;
        for (int i = 0; i < 4; i++) r = CastSlash(c, dummy);
        Assert.Equal("Slash used 4/5", q2.ProgressText);
        Assert.Empty(r.ReadyQuests);

        r = CastSlash(c, dummy);
        Assert.Single(r.ReadyQuests);
        Assert.Equal(QuestState.ReadyToTurnIn, q2.State);
        Assert.DoesNotContain("energy-ball", c.Abilities);

        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.Contains("energy-ball", c.Abilities);
        Assert.Equal(QuestState.Complete, q2.State);
    }

    // ---- Energy Ball

    [Fact]
    public void EnergyBallIsMagicDamageCostsFiveAndHasAFiveSecondCooldown()
    {
        var c = Make(false, staff: true);
        var d = Dummy();
        int before = c.Energy;
        Assert.Equal("energy-ball", c.Combat.Hotbar[1].AbilityId);

        var r = c.Combat.UseHotbar(1, 5, 5, new List<Enemy> { d });

        Assert.True(r.Succeeded);
        Assert.Equal(26, r.Damage);                         // (10 + 5 staff + 5) x Int 13 modifier 1.3 = 26
        Assert.Equal(before - 5, c.Energy);
        Assert.Equal(5.0, c.Combat.Hotbar[1].CooldownRemaining, 2);
        Assert.Equal(1, c.GetAbility("energy-ball").Uses);
        Assert.Equal(0, c.GetAbility("slash").Uses);
    }

    [Fact]
    public void EnergyBallNeedsAStaffInTheMainHand()   // #39, Dyllan: "Yes it does..."
    {
        var unarmed = Make(false);
        Assert.Equal(AbilityDefinition.NeedsStaffText, CombatController.CannotCastReason(unarmed, AbilityLibrary.EnergyBallDefinition));
        var r = unarmed.Combat.UseHotbar(1, 5, 5, new List<Enemy> { Dummy() });
        Assert.Equal(CombatOutcome.NeedsWeapon, r.Outcome);
        Assert.Equal("Needs a staff", r.Reason);
        Assert.Equal(unarmed.MaxEnergy, unarmed.Energy);                      // nothing spent

        var sword = Make(true);
        Assert.Equal("Needs a staff", CombatController.CannotCastReason(sword, AbilityLibrary.EnergyBallDefinition));   // a sword is not a staff

        var staff = Make(false, staff: true);
        Assert.Null(CombatController.CannotCastReason(staff, AbilityLibrary.EnergyBallDefinition));
        Assert.True(staff.Combat.UseHotbar(1, 5, 5, new List<Enemy> { Dummy() }).Succeeded);
    }

    [Fact]
    public void SlashStillWorksWithAStaffUntilDyllanSaysOtherwise()   // open question: does a staff count as a weapon for Slash (placeholder: yes)
    {
        var c = Make(false, staff: true);
        Assert.Null(CombatController.CannotCastReason(c, AbilityLibrary.SlashDefinition));
        Assert.Equal("Needs a weapon", CombatController.CannotCastReason(Make(false), AbilityLibrary.SlashDefinition));
    }

    [Fact]
    public void EnergyBallKillCountsTowardsSlimeQuests()
    {
        var c = Make(false, staff: true);
        var q = c.Quests.Offer(QuestLibrary.SliceAndSlime());
        c.Quests.Accept(q.Id);
        var slime = new Enemy(Enemy.SlimeType, 20);
        slime.TileX = 6; slime.TileY = 5;
        var r = c.Combat.UseHotbar(1, 5, 5, new List<Enemy> { slime });
        Assert.Equal(CombatOutcome.Killed, r.Outcome);           // 26 damage vs 20 HP
        Assert.Equal("Slimes slain 1/5", q.ProgressText);
    }
}
