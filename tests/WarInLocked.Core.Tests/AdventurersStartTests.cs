using WarInLocked.Core.Characters;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

/// <summary>#52 quest 5 "Adventurers start": offered by the Colonel after quest 4, completed by talking to Whiplash.</summary>
public class AdventurersStartTests
{
    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    private static string[] Lines(DialogueRunner r)
    {
        var list = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 10 && !r.IsEnded; i++)
        {
            list.Add(r.Text);
            if (r.HasOptions) break;
            r.Advance();
        }
        return list.ToArray();
    }

    /// <summary>Quests 1-4 handed in; quest 5 just offered (not accepted).</summary>
    private static Character AtQuest5Offer()
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
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        c.Quests.RecordPurchase(ItemLibrary.LesserHealingPotionId);
        var q5 = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out _);
        Assert.Equal(QuestLibrary.AdventurersStartId, q5!.Id);
        return c;
    }

    [Fact]
    public void QuestHasDyllansTextRewardAndNoFailure()
    {
        var q = QuestLibrary.AdventurersStart();
        Assert.Equal("Quest: Adventurers start.", q.Title);
        Assert.Equal("Enter the Adventurers camp and speak with Whiplash", q.Description);
        Assert.Equal("Whiplash spoken with 0/1", q.Objectives[0].ProgressText);
        Assert.Equal("whiplash", q.Objectives[0].NpcId);
        Assert.Equal(ObjectiveKind.Talk, q.Objectives[0].Kind);
        Assert.Equal(50, q.Reward.Xp);
        Assert.Equal(50, q.Reward.Copper);
        Assert.Equal("50 XP, 50 Copper", q.Reward.Text);
        Assert.Equal("None", q.FailureCondition);
        Assert.Equal("Adventurers start", q.ShortTitle);
        Assert.Equal(QuestState.Offered, q.State);
    }

    [Fact]
    public void ColonelOffersItWhenQuestFourIsHandedInWithDyllansLineVerbatim()
    {
        var c = Hero();
        // reach the quest 4 turn-in
        ColonelPowersQuest.QuestToOfferAfterDialogue(c); ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c); ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c); ColonelPowersQuest.Accept(c, out _);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("energy-ball");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c); ColonelPowersQuest.Accept(c, out _);
        c.Quests.RecordPurchase(ItemLibrary.LesserHealingPotionId);

        var lines = Lines(ColonelPowersQuest.CreateDialogue(c));   // quest 4 turn-in speech, ends in the offer
        Assert.Equal(DialogueLibrary.AdventurersSpeech2, lines[lines.Length - 1]);
        Assert.Equal(
            "Your barely passable but there will be plenty of time to train you up before the pilgrimage. Journey through the woods and enter the Adventurers encampment in the nearby clearing, speak with my colleague Whiplash. He will advise you moveing forward.",
            lines[lines.Length - 2] + " " + lines[lines.Length - 1]);

        var offer = ColonelPowersQuest.QuestToOfferAfterDialogue(c, out _);
        Assert.Equal(QuestLibrary.AdventurersStartId, offer!.Id);
        Assert.Equal(QuestState.Offered, offer.State);
    }

    [Fact]
    public void OfferRepeatsUntilAcceptedThenColonelShowsTheTalkShopChoice()
    {
        var c = AtQuest5Offer();
        Assert.Equal(DialogueLibrary.ColonelPowersAdventurersOfferId, ColonelPowersQuest.DialogueIdFor(c));
        var lines = Lines(ColonelPowersQuest.CreateDialogue(c));
        Assert.Equal(new[] { DialogueLibrary.AdventurersSpeech1, DialogueLibrary.AdventurersSpeech2 }, lines);

        Assert.True(ColonelPowersQuest.Accept(c, out var notice));
        Assert.Null(notice);
        Assert.Equal(QuestState.Active, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);
        Assert.Equal(DialogueLibrary.ColonelPowersMenuId, ColonelPowersQuest.DialogueIdFor(c));
        Assert.Null(ColonelPowersQuest.QuestToOfferAfterDialogue(c));
    }

    [Fact]
    public void TalkingToWhiplashCompletesTheQuestAndPaysOnTheSpot()
    {
        var c = AtQuest5Offer();
        ColonelPowersQuest.Accept(c, out _);
        int xp = c.Xp, copper = c.Copper;

        Assert.True(WhiplashQuest.AfterDialogue(c, out var notice));
        Assert.Equal("Quest complete: Adventurers start!  +50 XP, +50 copper", notice);
        var q = c.Quests.Get(QuestLibrary.AdventurersStartId)!;
        Assert.Equal(QuestState.Complete, q.State);
        Assert.Equal("Whiplash spoken with 1/1", q.Objectives[0].ProgressText);
        Assert.Equal(xp + 50, c.Xp);
        Assert.Equal(copper + 50, c.Copper);

        Assert.False(WhiplashQuest.AfterDialogue(c, out var again));   // once only
        Assert.Null(again);
        Assert.Equal(xp + 50, c.Xp);
        Assert.Equal(copper + 50, c.Copper);
        Assert.Equal(DialogueLibrary.ColonelPowersMenuId, ColonelPowersQuest.DialogueIdFor(c));
    }

    [Fact]
    public void TalkingBeforeAcceptingOrToSomeoneElseCountsNothing()
    {
        var c = AtQuest5Offer();
        Assert.False(WhiplashQuest.AfterDialogue(c, out _));                          // offered, not accepted
        Assert.Equal(0, c.Quests.Get(QuestLibrary.AdventurersStartId)!.Objectives[0].Current);

        ColonelPowersQuest.Accept(c, out _);
        Assert.Empty(c.Quests.NotifyTalkedTo("colonel-powers"));
        Assert.Equal(QuestState.Active, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);
    }

    [Fact]
    public void NotifyTalkedToIsTheGenericHookForTalkObjectives()
    {
        var c = Hero();
        var q = new Quest("t", "Quest: T", "d", new[] { QuestObjective.TalkTo("bob", "Bob spoken with") }, new QuestReward(10, null, 0));
        c.Quests.Offer(q);
        c.Quests.Accept("t");
        var ready = c.Quests.NotifyTalkedTo("bob");
        Assert.Single(ready);
        Assert.Equal(QuestState.ReadyToTurnIn, q.State);   // normal quests still wait for the hand-in
        Assert.True(c.Quests.TurnIn("t"));
    }

    [Fact]
    public void WhiplashDialogueIsDataDrivenAndFollowsTheQuest()
    {
        var c = AtQuest5Offer();
        Assert.True(DialogueLibrary.Exists("whiplash"));
        Assert.Equal(DialogueLibrary.WhiplashIdleId, WhiplashQuest.DialogueIdFor(c));   // not accepted yet
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal(DialogueLibrary.WhiplashId, WhiplashQuest.DialogueIdFor(c));

        var r = WhiplashQuest.CreateDialogue(c);
        Assert.Equal("Whiplash", r.Speaker);
        Assert.Equal(DialogueLibrary.WhiplashAdviceLine1, r.Text);
        WhiplashQuest.AfterDialogue(c, out _);
        Assert.Equal(DialogueLibrary.WhiplashIdleId, WhiplashQuest.DialogueIdFor(c));
        Assert.Contains("Aragorn", Lines(WhiplashQuest.CreateDialogue(c))[0]);
    }

    [Fact]
    public void CannotLeaveTheForestUntilTheColonelHasGivenAdventurersStart()
    {
        var c = Hero();
        Assert.False(c.CanLeaveForest);                       // nothing given yet
        Assert.Equal("Colonel Powers isn't finished with you yet.", Character.LeaveForestRefusal);

        var fresh = AtQuest5Offer();
        Assert.False(fresh.CanLeaveForest);                   // offered but not accepted

        ColonelPowersQuest.Accept(fresh, out _);
        Assert.True(fresh.CanLeaveForest);                    // accepted

        WhiplashQuest.AfterDialogue(fresh, out _);
        Assert.Equal(QuestState.Complete, fresh.Quests.Get(QuestLibrary.AdventurersStartId)!.State);
        Assert.True(fresh.CanLeaveForest);                    // completed still counts
    }

    private static void DieAndRespawn(Character c)
    {
        c.TakeDamage(c.MaxHealth + 10);
        Assert.True(c.IsDead);
        c.UpdateRespawn(Character.RespawnDelaySeconds);
        Assert.False(c.IsDead);
    }

    [Fact]
    public void DyingAndRespawningKeepsTheForestExitOpenOnceAccepted()
    {
        var c = AtQuest5Offer();
        DieAndRespawn(c);
        Assert.False(c.CanLeaveForest);                       // offered only: still closed after a death

        ColonelPowersQuest.Accept(c, out _);
        Assert.True(c.CanLeaveForest);
        DieAndRespawn(c);
        Assert.True(c.CanLeaveForest);
        Assert.Equal(QuestState.Active, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);   // progress kept
        DieAndRespawn(c);
        Assert.True(c.CanLeaveForest);
    }

    [Fact]
    public void DyingAndRespawningKeepsTheForestExitOpenAfterCompletion()
    {
        var c = AtQuest5Offer();
        ColonelPowersQuest.Accept(c, out _);
        WhiplashQuest.AfterDialogue(c, out _);
        Assert.Equal(QuestState.Complete, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);

        DieAndRespawn(c);
        Assert.True(c.CanLeaveForest);
        Assert.Equal(QuestState.Complete, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);
    }
}
