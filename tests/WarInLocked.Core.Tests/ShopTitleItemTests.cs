using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

/// <summary>#25 quest 4, #26 consumables, #27 titles, #28 shops.</summary>
public class ShopTitleItemTests
{
    private static Character Hero(StatDistribution d = StatDistribution.Warrior) =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = d }.Create();

    /// <summary>A hero with quest 4 offered (quests 1-3 turned in).</summary>
    private static Character AtQuest4Offer()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("energy-ball");
        var q4 = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out _);
        Assert.Equal(QuestLibrary.ShoppingBeforeAdventureId, q4!.Id);
        return c;
    }

    private static Character WithQuest4Active()
    {
        var c = AtQuest4Offer();
        Assert.True(ColonelPowersQuest.Accept(c, out _));
        c.SpendCopper(c.Copper);   // quest 3 paid 50 copper; start these tests broke
        return c;
    }

    // ---- #26 items

    [Fact]
    public void PotionHasDyllansDescriptionAndEffectLines()
    {
        var p = ItemLibrary.LesserHealingPotion;
        Assert.Equal("Potion of lesser healing", p.Name);
        Assert.Equal("A dull red colour. Tastes oddly like strawberries.", p.Description);
        Assert.Equal(50, p.HealAmount);
        Assert.Equal(5, p.UseCooldownSeconds);
        Assert.Equal(10, p.MaxStack);
        Assert.Equal(new[] { "Heals 50 HP.", "Cooldown after use 5s" }, p.EffectLines);
        Assert.Equal(p.EffectLines, p.DetailLines);
        Assert.True(p.IsUsable);
    }

    [Fact]
    public void PotionsStackInTheInventory()
    {
        var c = Hero();
        Assert.Equal(10, c.Inventory.Add(ItemLibrary.LesserHealingPotion, 10));
        Assert.Equal(c.Inventory.Capacity - 1, c.Inventory.FreeSlots);
    }

    [Fact]
    public void UsingAPotionHealsCapsAtMaxConsumesOneAndStartsTheCooldown()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 2);
        c.TakeDamage(30);
        int before = c.Health;
        var r = c.UseItem(ItemLibrary.LesserHealingPotionId);
        Assert.True(r.Success);
        Assert.Equal(30, r.Healed);                       // capped at max health
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(before + 30, c.Health);
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
        Assert.Equal(5, c.ItemCooldownRemaining);
    }

    [Fact]
    public void ThePotionHealsFiftyWhenThereIsRoom()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion);
        c.TakeDamage(80);
        Assert.Equal(50, c.UseItem(ItemLibrary.LesserHealingPotionId).Healed);
        Assert.Equal(c.MaxHealth - 30, c.Health);
    }

    [Fact]
    public void UsingAtFullHealthIsRefusedAndNothingIsSpent()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion);
        Assert.Equal("Already at full health", c.CanUse(ItemLibrary.LesserHealingPotionId));
        var r = c.UseItem(ItemLibrary.LesserHealingPotionId);
        Assert.False(r.Success);
        Assert.Equal("Already at full health", r.Reason);
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
        Assert.Equal(0, c.ItemCooldownRemaining);
    }

    [Fact]
    public void TheCooldownIsSharedAndTicksInCombatUpdate()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 3);
        c.TakeDamage(90);
        Assert.True(c.UseItem(ItemLibrary.LesserHealingPotionId).Success);
        c.TakeDamage(10);
        var again = c.UseItem(ItemLibrary.LesserHealingPotionId);
        Assert.False(again.Success);
        Assert.Equal("On cooldown", again.Reason);
        Assert.Equal(2, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
        c.Combat.Update(4.9);
        Assert.Equal("On cooldown", c.CanUse(ItemLibrary.LesserHealingPotionId));
        c.Combat.Update(0.2);
        Assert.Null(c.CanUse(ItemLibrary.LesserHealingPotionId));
        Assert.True(c.UseItem(ItemLibrary.LesserHealingPotionId).Success);
    }

    [Fact]
    public void ItemsThatAreMissingOrNotUsableAreRefused()
    {
        var c = Hero();
        Assert.Equal("None left", c.CanUse(ItemLibrary.LesserHealingPotionId));
        c.Inventory.Add(new Item("trinket", "Trinket", ItemType.Misc));
        Assert.Equal("Cannot be used", c.CanUse("trinket"));
        Assert.False(c.UseItem("trinket").Success);
        Assert.Equal(1, c.Inventory.CountOf("trinket"));
    }

    [Fact]
    public void HotbarKeysUseAPotionWithoutATargetAndShareTheCooldown()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 2);
        c.Combat.AssignItemToHotbar(3, ItemLibrary.LesserHealingPotionId);
        Assert.Equal(ItemLibrary.LesserHealingPotionId, c.Combat.Hotbar[3].ItemId);

        var full = c.Combat.UseHotbar(3, 0, 0, new List<Enemy>());
        Assert.Equal(CombatOutcome.ItemRefused, full.Outcome);
        Assert.Equal("Already at full health", full.ItemResult!.Reason);

        c.TakeDamage(60);
        var used = c.Combat.UseHotbar(3, 0, 0, new List<Enemy>());
        Assert.Equal(CombatOutcome.ItemUsed, used.Outcome);
        Assert.Equal(50, used.ItemResult!.Healed);
        Assert.Equal(5, c.Combat.Hotbar[3].CooldownRemaining);
        Assert.Equal("On cooldown", c.Combat.UseHotbar(3, 0, 0, new List<Enemy>()).ItemResult!.Reason);
    }

    [Fact]
    public void AssigningAnAbilityToAnItemSlotReplacesTheItem()
    {
        var c = Hero();
        c.Combat.AssignItemToHotbar(2, ItemLibrary.LesserHealingPotionId);
        c.Combat.Hotbar.Assign(2, "slash");
        Assert.Null(c.Combat.Hotbar[2].ItemId);
        Assert.Equal("slash", c.Combat.Hotbar[2].AbilityId);
    }

    // ---- #27 titles

    [Fact]
    public void AdventurerTitleHasDyllansTextAndBonus()
    {
        var t = TitleLibrary.Adventurer;
        Assert.Equal("Adventurer", t.Name);
        Assert.StartsWith("You have completed the tutorial. The Adventurer is one who is prepared for the upcoming adventure.", t.Description);
        Assert.Equal(TitleLibrary.AdventurerDescription + " " + TitleLibrary.AdventurerBonusExplanation, t.Description);   // #33: the milestone rule is in the text
        Assert.Contains("never takes a stat up to or past its next milestone (10, 25, 50)", t.Description);
        Assert.Equal(new[] { "+2 to all stats when equipped." }, t.BonusLines);
    }

    [Fact]
    public void TitlesMustBeEarnedAndOnlyOneIsWornAtATime()
    {
        var c = Hero();
        Assert.False(c.EquipTitle(TitleLibrary.AdventurerId));
        Assert.True(c.GrantTitle(TitleLibrary.AdventurerId));
        Assert.False(c.GrantTitle(TitleLibrary.AdventurerId));
        Assert.Single(c.Titles);
        Assert.Null(c.EquippedTitle);
        Assert.True(c.EquipTitle(TitleLibrary.AdventurerId));
        Assert.Equal(TitleLibrary.Adventurer, c.EquippedTitle);
        Assert.True(c.UnequipTitle());
        Assert.Null(c.EquippedTitle);
        Assert.False(c.UnequipTitle());
    }

    [Fact]
    public void EquippedTitleFeedsEffectiveStatsButNotTheAssignedStats()
    {
        var c = Hero();
        var baseStats = c.Stats;
        double conMod = c.Modifier(StatType.Constitution);
        int maxHp = c.MaxHealth, maxEn = c.MaxEnergy;
        c.GrantTitle(TitleLibrary.AdventurerId);
        Assert.Equal(maxHp, c.MaxHealth);                  // earned is not worn
        c.EquipTitle(TitleLibrary.AdventurerId);

        foreach (StatType s in System.Enum.GetValues(typeof(StatType)))
        {
            Assert.Equal(baseStats[s], c.Stats[s]);        // the I menu shows base + assigned
            int expected = StatModifier.LimitedBonus(baseStats[s], 2);   // #33: never up to or past the next milestone
            Assert.Equal(baseStats[s] + expected, c.EffectiveStats[s]);
            Assert.Equal(expected, c.TitleBonus(s));
        }
        Assert.True(c.Modifier(StatType.Constitution) > conMod);
        Assert.True(c.MaxHealth > maxHp);
        Assert.True(c.MaxEnergy > maxEn);
    }

    [Fact]
    public void EquippingAndUnequippingMoveCurrentHealthAndEnergyLikeAssigningPoints()
    {
        var c = Hero();
        c.TakeDamage(20);
        int hp = c.Health, maxHp = c.MaxHealth;
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        int gained = c.MaxHealth - maxHp;
        Assert.True(gained > 0);
        Assert.Equal(hp + gained, c.Health);
        c.UnequipTitle();
        Assert.Equal(maxHp, c.MaxHealth);
        Assert.Equal(hp, c.Health);
    }

    [Fact]
    public void UnequippingNeverKillsAndTheTitleBonusRaisesDamage()
    {
        var c = Hero();
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        c.TakeDamage(c.Health - 1);
        c.UnequipTitle();
        Assert.Equal(1, c.Health);

        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        int plain = DamageCalculator.PhysicalDamage(c);
        c.EquipTitle(TitleLibrary.AdventurerId);
        Assert.True(DamageCalculator.PhysicalDamage(c) >= plain);
    }

    [Fact]
    public void MilestonesCountEffectiveStats()
    {
        // #33: the title can no longer carry a stat onto the milestone: 8 + title = 9 (0.9), but a stat of 10 gets the full +2 (12 = 2.2).
        var c = new Character("Test", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(8));
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        Assert.Equal(StatModifier.Value(9), c.Modifier(StatType.Constitution));
        var d = new Character("Test", Race.Human, StatDistribution.Warrior, StatBlock.Uniform(10));
        d.GrantTitle(TitleLibrary.AdventurerId);
        d.EquipTitle(TitleLibrary.AdventurerId);
        Assert.Equal(StatModifier.Value(12), d.Modifier(StatType.Constitution));
    }

    // ---- #25 quest 4

    [Fact]
    public void QuestThreeTurnInSaysDyllansTwoLinesThenOffersQuestFour()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("energy-ball");

        var d = ColonelPowersQuest.CreateDialogue(c);
        Assert.Equal("you might just be a believable spell slinger.", d.Text);
        d.Advance();
        Assert.Equal("Now one cannot survive with out economic power, to prepare for adventure you must use your skills, or buy supplies that shall aid you. I just so happen to have a potion of healing on me. I shall sell it to you.", d.Text);
        d.Advance();
        Assert.True(d.IsEnded);

        var q4 = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out _)!;
        Assert.Equal("Quest: Shopping before adventure.", q4.Title);
        Assert.Equal("Colonel Powers has a potion of lesser healing he is willing to sell to you.", q4.Description);
        Assert.Equal("Potion of lesser healing baught 0/1", q4.ProgressText);
        Assert.Equal("Title Adventurer, 50 XP, Adventurer's rations", q4.Reward.Text);
        Assert.Equal("Colonel Powers", q4.TurnInNpcName);
        Assert.Equal(QuestState.Offered, q4.State);
        Assert.Equal("colonel-powers-shop-offer", ColonelPowersQuest.DialogueIdFor(c));   // re-offered with the same speech if declined
    }

    [Fact]
    public void QuestFourIsCompletedByBuyingAPotionAndPaysTitleXpAndRations()
    {
        var c = WithQuest4Active();
        var q4 = c.Quests.Get(QuestLibrary.ShoppingBeforeAdventureId);
        c.GainCopper(25);
        int xp = c.Xp;

        var buy = ShopLibrary.ColonelPowers().Buy(c, ItemLibrary.LesserHealingPotionId);
        Assert.True(buy.Success);
        Assert.Equal(QuestState.ReadyToTurnIn, q4.State);
        Assert.Single(buy.ReadyQuests);
        Assert.Equal("colonel-powers-shop-turnin", ColonelPowersQuest.DialogueIdFor(c));   // handed in first

        var next = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out var notice);   // #52: handing in quest 4 offers "Adventurers start"
        Assert.Equal(QuestLibrary.AdventurersStartId, next!.Id);
        Assert.Equal("Quest complete: Shopping before adventure!  title Adventurer, +50 XP, Adventurer's rations", notice);
        Assert.Equal(xp + 50, c.Xp);
        Assert.True(c.HasTitle(TitleLibrary.AdventurerId));
        Assert.Null(c.EquippedTitle);                                                   // earned, not worn
        Assert.Equal(1, c.Inventory.CountOf(ItemLibrary.AdventurersRationsId));
        Assert.Equal(QuestState.Complete, q4.State);
        Assert.Equal("colonel-powers-adventurers-offer", ColonelPowersQuest.DialogueIdFor(c));
        Assert.Null(ColonelPowersQuest.TurnInReadyQuest(c));
    }

    [Fact]
    public void QuestFourTurnInWaitsWhenTheInventoryHasNoRoomForTheRations()
    {
        var c = WithQuest4Active();
        c.Quests.RecordPurchase(ItemLibrary.LesserHealingPotionId);
        var q4 = c.Quests.Get(QuestLibrary.ShoppingBeforeAdventureId);
        Assert.Equal(QuestState.ReadyToTurnIn, q4.State);
        for (int i = 0; c.Inventory.FreeSlots > 0; i++) c.Inventory.Add(new Item("junk" + i, "Junk", ItemType.Misc));
        int xp = c.Xp;

        ColonelPowersQuest.QuestToOfferAfterDialogue(c, out var notice);

        Assert.Equal("Your inventory is full", notice);
        Assert.Equal(QuestState.ReadyToTurnIn, q4.State);
        Assert.Equal(xp, c.Xp);
        Assert.False(c.HasTitle(TitleLibrary.AdventurerId));

        c.Inventory.RemoveItem("junk0", 1);
        ColonelPowersQuest.QuestToOfferAfterDialogue(c, out notice);
        Assert.StartsWith("Quest complete:", notice);
        Assert.True(c.HasTitle(TitleLibrary.AdventurerId));
    }

    // ---- #28 shop and Talk / Shop menu

    [Fact]
    public void ColonelsShopSellsOnePotionForTwentyFiveCopper()
    {
        var shop = ShopLibrary.ColonelPowers();
        Assert.Equal("colonel-powers-shop", shop.Id);
        Assert.Single(shop.Stock);
        Assert.Equal(25, shop.PriceOf(ItemLibrary.LesserHealingPotionId));
        Assert.Equal("25c", shop.Stock[0].PriceText);
        Assert.True(shop.BuyEnabled);
        Assert.False(shop.SellEnabled);
        Assert.False(shop.HaggleEnabled);
        Assert.Equal(-1, shop.PriceOf("nothing"));
    }

    [Fact]
    public void BuyingRefusesWithoutCopperAndChangesNothing()
    {
        var c = WithQuest4Active();
        c.GainCopper(24);
        var shop = ShopLibrary.ColonelPowers();
        Assert.Equal("Not enough copper", shop.CanBuy(c, ItemLibrary.LesserHealingPotionId));
        var r = shop.Buy(c, ItemLibrary.LesserHealingPotionId);
        Assert.False(r.Success);
        Assert.Equal("Not enough copper", r.Reason);
        Assert.Equal(24, c.Copper);
        Assert.Equal(0, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
        Assert.Equal("Potion of lesser healing baught 0/1", c.Quests.Get(QuestLibrary.ShoppingBeforeAdventureId).ProgressText);
    }

    [Fact]
    public void BuyingRefusesWhenTheInventoryIsFullAndChangesNothing()
    {
        var c = WithQuest4Active();
        c.GainCopper(100);
        for (int i = 0; c.Inventory.FreeSlots > 0; i++) c.Inventory.Add(new Item("junk" + i, "Junk", ItemType.Misc));
        var r = ShopLibrary.ColonelPowers().Buy(c, ItemLibrary.LesserHealingPotionId);
        Assert.False(r.Success);
        Assert.Equal("Inventory full", r.Reason);
        Assert.Equal(100, c.Copper);
        Assert.Equal("Potion of lesser healing baught 0/1", c.Quests.Get(QuestLibrary.ShoppingBeforeAdventureId).ProgressText);
    }

    [Fact]
    public void BuyingSpendsCopperAddsTheItemAndStaysUnlimited()
    {
        var c = Hero();
        c.GainCopper(60);
        var shop = ShopLibrary.ColonelPowers();
        Assert.True(shop.Buy(c, ItemLibrary.LesserHealingPotionId).Success);
        Assert.True(shop.Buy(c, ItemLibrary.LesserHealingPotionId).Success);
        Assert.Equal(10, c.Copper);
        Assert.Equal(2, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
        Assert.Equal("Not for sale", shop.Buy(c, "nothing").Reason);
    }

    [Fact]
    public void TheColonelShowsTalkOrShopOnceQuestFourIsAccepted()
    {
        var c = WithQuest4Active();
        Assert.Equal("colonel-powers-menu", ColonelPowersQuest.DialogueIdFor(c));

        var talk = ColonelPowersQuest.CreateDialogue(c);
        Assert.Equal(new[] { "Talk", "Shop" }, talk.Options);
        talk.Choose(0);
        Assert.Equal("Stop waiting time and let's get moveing.", talk.Text);
        talk.Advance();
        Assert.True(talk.IsEnded);
        Assert.Null(talk.Action);

        var shop = ColonelPowersQuest.CreateDialogue(c);
        shop.Choose(1);
        Assert.True(shop.IsEnded);
        Assert.Equal("colonel-powers-shop", DialogueActions.ShopIdOf(shop.Action));
        Assert.NotNull(ShopLibrary.Get(DialogueActions.ShopIdOf(shop.Action)));
    }
}
