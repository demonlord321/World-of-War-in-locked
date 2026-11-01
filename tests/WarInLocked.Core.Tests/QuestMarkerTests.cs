using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

/// <summary>Quest markers over NPCs: yellow ! (available), grey ? (in progress), yellow ? (ready to turn in).</summary>
public class QuestMarkerTests
{
    private const string Colonel = "colonel-powers";
    private const string Whiplash = "whiplash";

    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    private static QuestMarker Col(Character c) => QuestMarkers.MarkerFor(Colonel, c);
    private static QuestMarker Whip(Character c) => QuestMarkers.MarkerFor(Whiplash, c);

    // Talk to the Colonel: offer (if any), accept it.
    private static void OfferAndAccept(Character c)
    {
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
    }

    [Fact]
    public void EnumHasTheFourMarkers()
    {
        Assert.Equal(new[] { "None", "Available", "InProgress", "ReadyToTurnIn" }, System.Enum.GetNames(typeof(QuestMarker)));
    }

    [Fact]
    public void FreshCharacterSeesAYellowExclamationOnTheColonelAndNothingOnWhiplash()
    {
        var c = Hero();
        Assert.Equal(QuestMarker.Available, Col(c));
        Assert.Equal(QuestMarker.None, Whip(c));
        Assert.Equal(QuestMarker.None, QuestMarkers.MarkerFor("barracks-master", c));
        Assert.Equal(QuestMarker.None, QuestMarkers.MarkerFor("nobody", c));
        Assert.Equal(QuestMarker.None, QuestMarkers.MarkerFor(null, c));
        Assert.Equal(QuestMarker.Available, c.Quests.MarkerFor(Colonel));   // same answer from the log
    }

    [Fact]
    public void OfferedButNotAcceptedStillShowsExclamation()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);        // shown, player says No
        Assert.Equal(QuestState.Offered, c.Quests.Get(QuestLibrary.SliceAndSlimeId)!.State);
        Assert.Equal(QuestMarker.Available, Col(c));
    }

    [Fact]
    public void ColonelWalksThroughAvailableInProgressReadyForEveryTutorialQuest()
    {
        var c = Hero();

        // Quest 1
        OfferAndAccept(c);
        Assert.Equal(QuestMarker.InProgress, Col(c));
        for (int i = 0; i < 4; i++) c.Quests.RecordKill("slime");
        Assert.Equal(QuestMarker.InProgress, Col(c));
        c.Quests.RecordKill("slime");
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);        // hand in; quest 2 is offered
        Assert.Equal(QuestMarker.Available, Col(c));

        // Quest 2
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal(QuestMarker.InProgress, Col(c));
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.Equal(QuestMarker.Available, Col(c));

        // Quest 3
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal(QuestMarker.InProgress, Col(c));
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("energy-ball");
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.Equal(QuestMarker.Available, Col(c));

        // Quest 4
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal(QuestMarker.InProgress, Col(c));
        c.Quests.RecordPurchase(ItemLibrary.LesserHealingPotionId);
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);        // hand in; quest 5 is offered
        Assert.Equal(QuestMarker.Available, Col(c));
        Assert.Equal(QuestMarker.None, Whip(c));                // not accepted yet: nothing to finish at Whiplash

        // Quest 5: the Colonel keeps a grey ?, Whiplash shows yellow ? while it is active
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal(QuestState.Active, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);
        Assert.Equal(QuestMarker.InProgress, Col(c));
        Assert.Equal(QuestMarker.ReadyToTurnIn, Whip(c));

        // Talking to Whiplash completes it on the spot: both markers go away.
        Assert.True(WhiplashQuest.AfterDialogue(c, out _));
        Assert.Equal(QuestState.Complete, c.Quests.Get(QuestLibrary.AdventurersStartId)!.State);
        Assert.Equal(QuestMarker.None, Whip(c));
        Assert.Equal(QuestMarker.None, Col(c));                 // everything done
    }

    [Fact]
    public void PrerequisiteMustBeCompleteBeforeTheNextQuestShowsExclamation()
    {
        var c = Hero();
        OfferAndAccept(c);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");   // quest 1 ready, not handed in
        // Quest 2 is not available until quest 1 is handed in: the marker is the yellow ? for quest 1.
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));
        Assert.Null(c.Quests.Get(QuestLibrary.SlashingPracticeId));
    }

    [Fact]
    public void ReadyToTurnInBeatsAvailableAndInProgress()
    {
        var c = Hero();
        // Quest 1 handed in and quest 2 offered (Available), then quest 1-style conflict is simulated: quest 2 active and done while quest 3 would be next.
        OfferAndAccept(c);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.Equal(QuestMarker.Available, Col(c));
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal(QuestMarker.InProgress, Col(c));
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse("slash");
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));

        // Available beats InProgress: an offered (declined) quest next to an active one for the same NPC.
        var d = Hero();
        d.Quests.Offer(QuestLibrary.SliceAndSlime());
        d.Quests.Accept(QuestLibrary.SliceAndSlimeId);
        d.Quests.Offer(QuestLibrary.SlashingPractice());                // listed but not accepted
        Assert.Equal(QuestMarker.Available, Col(d));
    }

    [Fact]
    public void AReadyQuestWhoseRewardDoesNotFitStillShowsYellowQuestionMark()
    {
        var c = Hero();
        OfferAndAccept(c);
        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        Assert.Equal(QuestMarker.ReadyToTurnIn, Col(c));
        Assert.Equal(QuestMarker.ReadyToTurnIn, c.Quests.MarkerFor(Colonel));
    }

    [Fact]
    public void WhiplashShowsNothingWhenTheQuestIsDoneOrUnknown()
    {
        var c = Hero();
        Assert.Equal(QuestMarker.None, Whip(c));
        Assert.Throws<System.ArgumentNullException>(() => QuestMarkers.MarkerFor(Whiplash, null!));
    }

    [Fact]
    public void QuestGiverAndTurnInDataIsPerQuest()
    {
        Assert.Equal(Colonel, QuestLibrary.GiverNpcId(QuestLibrary.AdventurersStartId));
        Assert.Equal(Whiplash, QuestLibrary.TurnInNpcId(QuestLibrary.AdventurersStartId));
        Assert.Equal(Colonel, QuestLibrary.TurnInNpcId(QuestLibrary.SliceAndSlimeId));
        Assert.Null(QuestLibrary.PrerequisiteId(QuestLibrary.SliceAndSlimeId));
        Assert.Equal(QuestLibrary.ShoppingBeforeAdventureId, QuestLibrary.PrerequisiteId(QuestLibrary.AdventurersStartId));
        Assert.Null(QuestLibrary.GiverNpcId("nope"));
    }
}
