using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

/// <summary>#21 quest 3 "Energy Ball practice", #22 weapon stats and descriptions, copper.</summary>
public class EnergyBallQuestTests
{
    private static Character Hero(StatDistribution d = StatDistribution.Warrior) =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = d }.Create();

    /// <summary>A hero who has turned in quests 1 and 2 and is now offered quest 3.</summary>
    private static Character AtQuest3Offer()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");
        var q3 = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out _);
        Assert.Equal(QuestLibrary.EnergyBallPracticeId, q3.Id);
        return c;
    }

    // ---- items (#22)

    [Fact]
    public void PracticeSwordHasPhysicalAttackAndDyllansDescription()
    {
        var s = ItemLibrary.PracticeSword;
        Assert.Equal(5, s.PhysicalAttack);
        Assert.Equal(0, s.MagicalAttack);
        Assert.Equal("Not overly impressive, the practice sword has a dull edge but for a novice is still useful to learn the craft.", s.Description);
        Assert.Equal(new[] { "Physical Attack +5" }, s.BonusLines);
        Assert.Equal(EquipSlot.MainHand, s.EquipSlot);
    }

    [Fact]
    public void PracticeStaffIsAMainHandWeaponWithMagicalAttackAndDyllansDescription()
    {
        var s = ItemLibrary.PracticeStaff;
        Assert.Equal("Practice Staff", s.Name);
        Assert.Equal(ItemType.Weapon, s.Type);
        Assert.Equal(EquipSlot.MainHand, s.EquipSlot);
        Assert.Equal(0, s.PhysicalAttack);
        Assert.Equal(5, s.MagicalAttack);
        Assert.Equal("Looks somewhat like driftwood... But don't let its looks deceive you, it still does what it needs to do...", s.Description);
        Assert.Equal(new[] { "Magical Attack +5" }, s.BonusLines);
    }

    [Fact]
    public void ItemsWithoutBonusesHaveNoBonusLinesAndAnEmptyDescription()
    {
        var rock = new Item("rock", "Rock", ItemType.Misc);
        Assert.Empty(rock.BonusLines);
        Assert.Equal("", rock.Description);
        Assert.Same(ItemLibrary.PracticeStaff, ItemLibrary.Get(ItemLibrary.PracticeStaffId));
        Assert.Null(ItemLibrary.Get("nope"));
        Assert.Contains(ItemLibrary.PracticeSword, ItemLibrary.All);
        Assert.Contains(ItemLibrary.PracticeStaff, ItemLibrary.All);
    }

    [Fact]
    public void SlashWorksWithAStaffUsingPhysicalBasePlainTen()
    {
        var c = Hero();   // Str 13 = 2.3
        c.LearnAbility(AbilityLibrary.Slash);
        c.Equipment.AddAndEquip(ItemLibrary.PracticeStaff);
        Assert.Null(CombatController.CannotCastReason(c, AbilityLibrary.SlashDefinition));
        Assert.Equal(35, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition));   // (10 + 0 + 5) x 2.3 = 34.5 -> 35: the staff adds nothing physical
    }

    // ---- copper (#21)

    [Fact]
    public void CopperStartsAtZeroAndCanBeGainedAndSpent()
    {
        var c = Hero();
        Assert.Equal(0, c.Copper);
        int v = c.Version;
        int seen = -1;
        c.CopperChanged += total => seen = total;

        Assert.Equal(30, c.GainCopper(30));
        Assert.Equal(30, seen);
        Assert.Equal(v + 1, c.Version);
        Assert.True(c.CanAfford(30));
        Assert.False(c.CanAfford(31));
        Assert.False(c.SpendCopper(31));
        Assert.Equal(30, c.Copper);                  // nothing spent
        Assert.True(c.SpendCopper(10));
        Assert.Equal(20, c.Copper);
        Assert.Equal(20, seen);
        Assert.Equal(20, c.GainCopper(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.GainCopper(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.SpendCopper(-1));
    }

    // ---- quest 3 (#21)

    [Fact]
    public void QuestThreeUsesDyllansText()
    {
        var q = QuestLibrary.EnergyBallPractice();
        Assert.Equal("Quest: Energy Ball practice.", q.Title);
        Assert.Equal("You have been asked to perform Energy Ball five times. Equip your staff and get to casting.", q.Description);
        Assert.Equal("Energy ball 0/5", q.ProgressText);
        Assert.Equal("50 XP, 50 Copper", q.Reward.Text);
        Assert.Equal("None", q.FailureCondition);
        Assert.Equal("Energy Ball practice", q.ShortTitle);
    }

    [Fact]
    public void TurningInQuestTwoSaysTheSpeechInThreeBoxesThenOffersQuestThree()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");

        var d = ColonelPowersQuest.CreateDialogue(c);
        var spoken = new List<string>();
        while (!d.IsEnded)
        {
            spoken.Add(d.Text);
            d.Advance();
        }
        Assert.Equal(3, spoken.Count);
        Assert.Equal(
            "You are starting to learn, you might just survive this war-torn world... Congrats on getting your first spell. " +
            "Now take this and show me you can use the wonderful art that is magic... " +
            "The energy is within you, you have used it for your slash... Now manifest it as an attack.",
            string.Join(" ", spoken));
    }

    [Fact]
    public void AcceptingQuestThreePutsThePracticeStaffInTheInventoryOnce()
    {
        var c = AtQuest3Offer();
        Assert.Equal(0, c.Inventory.CountOf(ItemLibrary.PracticeStaffId));

        Assert.True(ColonelPowersQuest.Accept(c, out var notice));

        Assert.Equal("Press I to open your character sheet and equip the staff.", notice);   // #39 popup text
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.PracticeStaffId));
        Assert.Equal(ItemLibrary.PracticeSwordId, c.Equipment.Get(EquipmentSlot.MainHand).Id);   // given, not equipped: the player equips it
        var q3 = c.Quests.Get(QuestLibrary.EnergyBallPracticeId);
        Assert.Equal(QuestState.Active, q3.State);
        Assert.Equal("Energy ball 0/5", q3.ProgressText);
    }

    [Fact]
    public void AFullInventoryRefusesQuestThreeAndKeepsItOffered()
    {
        var c = AtQuest3Offer();
        while (!c.Inventory.IsFull) c.Inventory.Add(new Item("rock" + c.Inventory.Capacity, "Rock", ItemType.Misc, maxStack: 1));

        Assert.False(ColonelPowersQuest.Accept(c, out var notice));
        Assert.Equal("Your inventory is full", notice);
        var q3 = c.Quests.Get(QuestLibrary.EnergyBallPracticeId);
        Assert.Equal(QuestState.Offered, q3.State);
        Assert.False(c.HasFlag(ColonelPowersQuest.StaffGivenFlag));

        c.Inventory.Remove(0);   // make room, talk to the Colonel again
        Assert.Equal("colonel-powers-energy-offer", ColonelPowersQuest.DialogueIdFor(c));
        Assert.Same(q3, ColonelPowersQuest.QuestToOfferAfterDialogue(c));
        Assert.True(ColonelPowersQuest.Accept(c, out var ok));
        Assert.Equal(ColonelPowersQuest.StaffNotice, ok);
        Assert.Equal(QuestState.Active, q3.State);
    }

    [Fact]
    public void QuestThreeCountsEnergyBallCastsOnEnemiesAndPaysXpAndCopper()
    {
        var c = AtQuest3Offer();
        ColonelPowersQuest.Accept(c, out _);
        c.Equipment.AddAndEquip(ItemLibrary.PracticeStaff);
        c.Combat.Update(0.016);
        var q3 = c.Quests.Get(QuestLibrary.EnergyBallPracticeId);

        // Slash casts do not count; Energy Ball casts that fire do (5 s cooldown each).
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal("Energy ball " + i + "/5", q3.ProgressText);
            var slime = new Enemy(Enemy.SlimeType, 1000) { TileX = 6, TileY = 5 };
            int slot = c.Combat.Hotbar.IndexOf(AbilityLibrary.EnergyBall);
            Assert.True(slot >= 0);
            Assert.Equal(CombatOutcome.Hit, c.Combat.UseHotbar(slot, 5, 5, new List<Enemy> { slime }).Outcome);
            for (int t = 0; t < 120; t++) c.Combat.Update(0.05);
        }
        Assert.Equal(QuestState.ReadyToTurnIn, q3.State);

        int xp = c.Xp;
        Assert.Equal("colonel-powers-energy-turnin", ColonelPowersQuest.DialogueIdFor(c));
        var next = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out var notice);   // #25: turning it in offers quest 4
        Assert.Equal(QuestLibrary.ShoppingBeforeAdventureId, next!.Id);
        Assert.Equal("Quest complete: Energy Ball practice!  +50 XP, +50 copper", notice);
        Assert.Equal(xp + 50, c.Xp);
        Assert.Equal(50, c.Copper);
        Assert.Equal("colonel-powers-shop-offer", ColonelPowersQuest.DialogueIdFor(c));
        Assert.Null(ColonelPowersQuest.TurnInReadyQuest(c));   // no second reward
        Assert.Equal(50, c.Copper);
    }

    [Fact]
    public void QuestThreeActiveDialogueShowsProgress()
    {
        var c = AtQuest3Offer();
        ColonelPowersQuest.Accept(c, out _);
        c.Quests.RecordAbilityUse(AbilityLibrary.EnergyBall);
        c.Quests.RecordAbilityUse(AbilityLibrary.EnergyBall);
        Assert.Equal("Keep at it, Aragorn... Energy ball 2/5.", ColonelPowersQuest.CreateDialogue(c).Text);
    }
}
