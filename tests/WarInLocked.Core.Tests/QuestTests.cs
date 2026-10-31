using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Tests;

public class QuestTests
{
    private static Character Hero() =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();

    private static Character HeroWithActiveQuest()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.True(ColonelPowersQuest.Accept(c, out _));
        return c;
    }

    private static List<(int X, int Y)> SlimesAround(int px, int py, int n)
    {
        var list = new List<(int X, int Y)>();
        for (int i = 0; i < n; i++) list.Add((px + 1, py + (i % 2 == 0 ? 0 : 1)));
        return list;
    }

    [Fact]
    public void SliceAndSlimeUsesDyllansText()
    {
        var q = QuestLibrary.SliceAndSlime();
        Assert.Equal("Quest: Slice and slime.", q.Title);
        Assert.Equal("Colonel Powers has requested you hunt down 5 of the slimes in the area.", q.Description);
        Assert.Equal("Slimes slain 0/5", q.ProgressText);
        Assert.Equal("50 XP, Ability Slash", q.Reward.Text);
        Assert.Equal("None", q.FailureCondition);
        Assert.Equal(QuestState.Offered, q.State);
    }

    [Fact]
    public void ProgressCountsOnlyMatchingEnemiesInActiveQuests()
    {
        var c = Hero();
        var q = c.Quests.Offer(QuestLibrary.SliceAndSlime());

        c.Quests.RecordKill("slime");                      // offered, not active: no progress
        Assert.Equal(0, q.Objectives[0].Current);

        c.Quests.Accept(q.Id);
        c.Quests.RecordKill("wolf");
        Assert.Equal(0, q.Objectives[0].Current);
        c.Quests.RecordKill("slime");
        c.Quests.RecordKill("slime");
        Assert.Equal("Slimes slain 2/5", q.ProgressText);
        Assert.Equal(QuestState.Active, q.State);
    }

    [Fact]
    public void FifthSlimeCompletesTheQuestAndGivesTheRewardsOnce()
    {
        var c = Hero();
        var q = c.Quests.Offer(QuestLibrary.SliceAndSlime());
        c.Quests.Accept(q.Id);

        for (int i = 0; i < 4; i++) Assert.Empty(c.Quests.RecordKill("slime"));
        Assert.Equal(0, c.Xp);

        var done = c.Quests.RecordKill("slime");

        Assert.Single(done);
        Assert.Equal(QuestState.Complete, q.State);
        Assert.Equal(50, c.Xp);
        Assert.Contains("slash", c.Abilities);

        Assert.Empty(c.Quests.RecordKill("slime"));        // no double reward, no 6/5
        Assert.Equal(50, c.Xp);
        Assert.Equal(5, q.Objectives[0].Current);
        Assert.Single(c.Abilities);
    }

    [Fact]
    public void OfferingTwiceKeepsTheSameQuestAndAcceptWorksOnce()
    {
        var c = Hero();
        var a = c.Quests.Offer(QuestLibrary.SliceAndSlime());
        var b = c.Quests.Offer(QuestLibrary.SliceAndSlime());
        Assert.Same(a, b);
        Assert.True(c.Quests.Accept(a.Id));
        Assert.False(c.Quests.Accept(a.Id));
        Assert.False(c.Quests.Accept("nope"));
    }

    [Fact]
    public void AcceptingGivesAndEquipsThePracticeSwordWithTheNotice()
    {
        var c = Hero();
        Assert.Equal(QuestState.Offered, ColonelPowersQuest.QuestToOfferAfterDialogue(c).State);
        Assert.False(c.Inventory.Contains("practice-sword"));       // nothing before Yes

        Assert.True(ColonelPowersQuest.Accept(c, out var notice));

        Assert.Equal("Practice sword given", notice);
        Assert.Equal("Practice Sword", c.Equipment.Get(EquipmentSlot.MainHand).Name);
        Assert.Equal(ItemType.Weapon, c.Equipment.Get(EquipmentSlot.MainHand).Type);
        Assert.Equal(QuestState.Active, c.Quests.Get(QuestLibrary.SliceAndSlimeId).State);
    }

    [Fact]
    public void SayingNoGivesNothingAndTheColonelOffersAgainWithoutTheIntro()
    {
        var c = Hero();
        Assert.Equal("colonel-powers", ColonelPowersQuest.DialogueIdFor(c));

        var offered = ColonelPowersQuest.QuestToOfferAfterDialogue(c);   // intro done, player says No: nothing else happens
        Assert.NotNull(offered);
        Assert.Null(c.Equipment.Get(EquipmentSlot.MainHand));
        Assert.Equal(10, c.Inventory.FreeSlots);

        Assert.Equal("colonel-powers-reoffer", ColonelPowersQuest.DialogueIdFor(c));
        var talk = ColonelPowersQuest.CreateDialogue(c);
        Assert.Equal(DialogueLibrary.LackSkillLine, talk.Text);          // straight to the quest, no greeting
        Assert.False(talk.HasOptions);
        talk.Advance();
        Assert.True(talk.IsEnded);
        Assert.Same(offered, ColonelPowersQuest.QuestToOfferAfterDialogue(c));   // prompt opens again
    }

    [Fact]
    public void TheSwordIsOnlyGivenOnce()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        Assert.True(ColonelPowersQuest.Accept(c, out var first));
        Assert.Equal("Practice sword given", first);

        // The player drops the sword; later the sword flag still says it was given, so no second one appears.
        c.Equipment.Unequip(EquipmentSlot.MainHand);
        c.Inventory.Remove(0);
        Assert.True(c.HasFlag(ColonelPowersQuest.SwordGivenFlag));

        var other = Hero();
        other.SetFlag(ColonelPowersQuest.SwordGivenFlag);
        ColonelPowersQuest.QuestToOfferAfterDialogue(other);
        Assert.True(ColonelPowersQuest.Accept(other, out var second));
        Assert.Null(second);
        Assert.False(other.Inventory.Contains("practice-sword"));
        Assert.Null(other.Equipment.Get(EquipmentSlot.MainHand));
    }

    [Fact]
    public void DialogueChoiceFollowsQuestState()
    {
        var c = Hero();
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);
        ColonelPowersQuest.Accept(c, out _);
        Assert.Equal("colonel-powers-active", ColonelPowersQuest.DialogueIdFor(c));
        Assert.Null(ColonelPowersQuest.QuestToOfferAfterDialogue(c));     // no prompt while active
        Assert.Equal("Keep at it, Aragorn... Slimes slain 0/5.", ColonelPowersQuest.CreateDialogue(c).Text);

        for (int i = 0; i < 5; i++) c.Quests.RecordKill("slime");
        Assert.Equal("colonel-powers-done", ColonelPowersQuest.DialogueIdFor(c));
        Assert.Null(ColonelPowersQuest.QuestToOfferAfterDialogue(c));
    }

    [Fact]
    public void AcceptFailsWithoutRoomForTheSwordAndKeepsTheQuestOffered()
    {
        var c = Hero();
        for (int i = 0; i < 10; i++) c.Inventory.Add(new Item("rock", "Rock", ItemType.Misc));
        c.Equipment.AddAndEquip(new Item("old", "Old Sword", ItemType.Weapon, EquipSlot.MainHand));
        ColonelPowersQuest.QuestToOfferAfterDialogue(c);

        Assert.False(ColonelPowersQuest.Accept(c, out var notice));

        Assert.Equal("Your inventory is full", notice);
        Assert.Equal(QuestState.Offered, c.Quests.Get(QuestLibrary.SliceAndSlimeId).State);
        Assert.False(c.HasFlag(ColonelPowersQuest.SwordGivenFlag));
    }

    [Fact]
    public void ForestHasEnoughSlimesForTheQuest()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string path = null;
        while (dir != null && path == null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "WarInLocked.Game", "Assets", "Maps", "forest-of-beginnings.txt");
            if (File.Exists(candidate)) path = candidate;
            dir = dir.Parent;
        }
        var map = WarInLocked.Core.World.TileMap.Parse(File.ReadAllText(path));
        Assert.True(map.SlimeSpawns.Count >= 5);
    }

    [Fact]
    public void CharacterXpAndAbilities()
    {
        var c = Hero();
        Assert.Equal(0, c.Xp);
        c.GainXp(10);
        Assert.Equal(10, c.Xp);
        Assert.Throws<ArgumentOutOfRangeException>(() => c.GainXp(-1));
        Assert.True(c.LearnAbility("slash"));
        Assert.False(c.LearnAbility("slash"));
    }
}
