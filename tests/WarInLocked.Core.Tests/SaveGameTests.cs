using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;
using WarInLocked.Core.Saving;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#54 save and load.</summary>
public class SaveGameTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wil-save-" + Guid.NewGuid().ToString("N"));
    private string File1 => Path.Combine(_dir, "sub", "save.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private static Character Rich()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Elf, Distribution = StatDistribution.Mystic }.Create();
        c.GainXp((int)Leveling.TotalXpFor(4) + 7);
        c.AssignPoint(StatType.Constitution);
        c.AssignPoint(StatType.Wisdom);
        c.AssignPoint(StatType.Wisdom);
        c.GainCopper(12345);
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 3);
        c.Inventory.Add(ItemLibrary.AdventurersRations, 2);
        c.Inventory.Swap(0, 5);
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        c.Inventory.Add(ItemLibrary.PracticeStaff);
        c.LearnAbility(AbilityLibrary.Slash);
        c.LearnAbility(AbilityLibrary.EnergyBall);
        for (int i = 0; i < 7; i++) c.GetAbility(AbilityLibrary.Slash).RecordUse();   // level 2, 2 into it
        c.Combat.SyncHotbar();
        c.Combat.AssignItemToHotbar(4, ItemLibrary.LesserHealingPotionId);
        var q = c.Quests.Offer(QuestLibrary.SliceAndSlime());
        c.Quests.Accept(q.Id);
        c.Quests.RecordKill("slime");
        c.Quests.RecordKill("slime");
        c.Quests.Offer(QuestLibrary.SlashingPractice());
        c.GrantTitle(TitleLibrary.AdventurerId);
        c.EquipTitle(TitleLibrary.AdventurerId);
        c.SetFlag("seen-intro");
        c.SetBindPoint(new BindPoint(MapIds.AdventurersEncampment, 33, 12));
        c.Clock.Advance(30.5);
        c.TakeDamage(20);
        c.SpendEnergy(5);
        return c;
    }

    private static void AssertSame(Character a, Character b)
    {
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Race, b.Race);
        Assert.Equal(a.Distribution, b.Distribution);
        foreach (var s in StatTypes.All)
        {
            Assert.Equal(a.Stats[s], b.Stats[s]);
            Assert.Equal(a.AssignedPoints(s), b.AssignedPoints(s));
        }
        Assert.Equal(a.FreeStatPoints, b.FreeStatPoints);
        Assert.Equal(a.Level, b.Level);
        Assert.Equal(a.Xp, b.Xp);
        Assert.Equal(a.XpIntoLevel, b.XpIntoLevel);
        Assert.Equal(a.MaxHealth, b.MaxHealth);
        Assert.Equal(a.MaxEnergy, b.MaxEnergy);
        Assert.Equal(a.Health, b.Health);
        Assert.Equal(a.EnergyExact, b.EnergyExact, 6);
        Assert.Equal(a.Copper, b.Copper);
        for (int i = 0; i < a.Inventory.Capacity; i++)
        {
            Assert.Equal(a.Inventory.Get(i)?.Item.Id, b.Inventory.Get(i)?.Item.Id);
            Assert.Equal(a.Inventory.Get(i)?.Count, b.Inventory.Get(i)?.Count);
        }
        foreach (var slot in EquipmentSlots.All) Assert.Equal(a.Equipment.Get(slot)?.Id, b.Equipment.Get(slot)?.Id);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(a.Combat.Hotbar[i].AbilityId, b.Combat.Hotbar[i].AbilityId);
            Assert.Equal(a.Combat.Hotbar[i].ItemId, b.Combat.Hotbar[i].ItemId);
        }
        Assert.Equal(a.Abilities, b.Abilities);
        foreach (var id in a.Abilities)
        {
            Assert.Equal(a.GetAbility(id).Level, b.GetAbility(id).Level);
            Assert.Equal(a.GetAbility(id).Uses, b.GetAbility(id).Uses);
            Assert.Equal(a.GetAbility(id).UsesIntoLevel, b.GetAbility(id).UsesIntoLevel);
        }
        Assert.Equal(a.Quests.All.Count, b.Quests.All.Count);
        foreach (var q in a.Quests.All)
        {
            var r = b.Quests.Get(q.Id);
            Assert.NotNull(r);
            Assert.Equal(q.State, r.State);
            Assert.Equal(q.ProgressText, r.ProgressText);
        }
        Assert.Equal(a.Titles.Select(t => t.Id), b.Titles.Select(t => t.Id));
        Assert.Equal(a.EquippedTitle?.Id, b.EquippedTitle?.Id);
        Assert.Equal(a.Flags.OrderBy(x => x), b.Flags.OrderBy(x => x));
        Assert.Equal(a.BindPoint, b.BindPoint);
        Assert.Equal(a.Clock.TotalMinutes, b.Clock.TotalMinutes, 6);
        Assert.Equal(a.Clock.Text, b.Clock.Text);
    }

    [Fact]
    public void RichCharacterSurvivesCaptureAndRestore()
    {
        var c = Rich();
        var back = SaveGame.Capture(c, MapIds.Forest, 24, 13).ToCharacter();
        AssertSame(c, back);
        // Spot checks that the setup really exercised everything.
        Assert.Equal(4, back.Level);
        Assert.Equal(12345, back.Copper);
        Assert.Equal(2, back.GetAbility(AbilityLibrary.Slash).Level);
        Assert.Equal(QuestState.Active, back.Quests.Get(QuestLibrary.SliceAndSlimeId).State);
        Assert.Equal("Slimes slain 2/5", back.Quests.Get(QuestLibrary.SliceAndSlimeId).ProgressText);
        Assert.Equal(QuestState.Offered, back.Quests.Get(QuestLibrary.SlashingPracticeId).State);
        Assert.Equal(ItemLibrary.PracticeSwordId, back.Equipment.Get(EquipmentSlot.MainHand).Id);
        Assert.Equal(ItemLibrary.LesserHealingPotionId, back.Combat.Hotbar[4].ItemId);
        Assert.True(back.HasFlag("seen-intro"));
        Assert.Equal(new BindPoint(MapIds.AdventurersEncampment, 33, 12), back.BindPoint);
    }

    [Fact]
    public void CurrentPositionComesBackInTheSave()
    {
        var s = SaveGame.Capture(Rich(), MapIds.AdventurersEncampment, 22, 8);
        Assert.Equal(MapIds.AdventurersEncampment, s.MapId);
        Assert.Equal(22, s.TileX);
        Assert.Equal(8, s.TileY);
    }

    [Fact]
    public void FileRoundTripCreatesFolderAndKeepsEverything()
    {
        var c = Rich();
        SaveGame.Save(c, MapIds.Forest, 24, 13, File1);
        Assert.True(File.Exists(File1));
        Assert.False(File.Exists(File1 + ".tmp"));
        var loaded = SaveGame.Load(File1);
        Assert.Equal(SaveGame.CurrentVersion, loaded.Version);
        Assert.Equal(MapIds.Forest, loaded.MapId);
        Assert.Equal(24, loaded.TileX);
        AssertSame(c, loaded.ToCharacter());
    }

    [Fact]
    public void JsonIsReadableAndHasAVersionField()
    {
        string json = SaveGame.Capture(Rich(), MapIds.Forest, 1, 2).ToJson();
        Assert.Contains("\"Version\": 1", json);
        Assert.Contains("\"Race\": \"Elf\"", json);
        Assert.Contains("\"ItemId\": \"potion-lesser-healing\"", json);
    }

    [Fact]
    public void SavingAgainOverwritesTheOldSave()
    {
        var c = Rich();
        SaveGame.Save(c, MapIds.Forest, 1, 1, File1);
        c.GainCopper(5);
        SaveGame.Save(c, MapIds.AdventurersEncampment, 2, 2, File1);
        var loaded = SaveGame.Load(File1);
        Assert.Equal(12350, loaded.Copper);
        Assert.Equal(MapIds.AdventurersEncampment, loaded.MapId);
    }

    [Fact]
    public void FreshCharacterRoundTrips()
    {
        var c = new CharacterCreation { Name = "Bob", Race = Race.Dwarf, Distribution = StatDistribution.Merchant }.Create();
        AssertSame(c, SaveGame.FromJson(SaveGame.Capture(c, MapIds.Forest, 22, 13).ToJson()).ToCharacter());
    }

    [Fact]
    public void MissingFileGivesASaveGameException()
    {
        Assert.False(SaveGame.Exists(File1));
        var e = Assert.Throws<SaveGameException>(() => SaveGame.Load(File1));
        Assert.Equal("No save found.", e.Message);
        Assert.False(SaveGame.TryLoad(File1, out var s, out var err));
        Assert.Null(s);
        Assert.Equal("No save found.", err);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"Version\": 1, \"Name\": ")]
    [InlineData("[1,2]")]
    [InlineData("{}")]
    public void DamagedFilesAreRefused(string text)
    {
        Directory.CreateDirectory(_dir);
        string p = Path.Combine(_dir, "bad.json");
        File.WriteAllText(p, text);
        Assert.False(SaveGame.TryLoad(p, out var loaded, out var err));
        Assert.Null(loaded);
        Assert.Equal("This save is damaged.", err);
    }

    [Fact]
    public void NewerVersionIsRefused()
    {
        string json = SaveGame.Capture(Rich(), MapIds.Forest, 1, 1).ToJson().Replace("\"Version\": 1", "\"Version\": 99");
        var e = Assert.Throws<SaveGameException>(() => SaveGame.FromJson(json));
        Assert.Contains("newer", e.Message);
    }

    [Fact]
    public void UnknownIdsInASaveAreSkipped()
    {
        var s = SaveGame.Capture(Rich(), MapIds.Forest, 1, 1);
        s.Inventory.Add(new SavedItemStack { Slot = 9, ItemId = "mystery", Count = 1 });
        s.Quests.Add(new SavedQuest { Id = "mystery-quest", State = QuestState.Active });
        s.Titles.Add("mystery-title");
        var c = s.ToCharacter();
        Assert.Null(c.Inventory.Get(9));
        Assert.Null(c.Quests.Get("mystery-quest"));
        Assert.False(c.HasTitle("mystery-title"));
    }

    [Fact]
    public void SaveMadeWhileFallenLoadsWithOneHealth()
    {
        var c = Rich();
        c.TakeDamage(10000);
        Assert.True(c.IsDead);
        var back = SaveGame.Capture(c, MapIds.Forest, 1, 1).ToCharacter();
        Assert.Equal(1, back.Health);
        Assert.False(back.IsDead);
    }

    [Fact]
    public void LoadedCharacterKeepsPlaying()
    {
        var back = SaveGame.Capture(Rich(), MapIds.Forest, 1, 1).ToCharacter();
        double before = back.Clock.TotalMinutes;
        back.Combat.Update(60);                    // the clock ticks on from the saved time
        Assert.True(back.Clock.TotalMinutes > before);
        back.GetAbility(AbilityLibrary.Slash).RecordUse();
        Assert.Equal(3, back.GetAbility(AbilityLibrary.Slash).UsesIntoLevel);
        int free = back.FreeStatPoints;
        Assert.Equal(free > 0, back.AssignPoint(StatType.Luck));
    }

    [Fact]
    public void SavedQuestInTurnInStateKeepsItsState()
    {
        var c = new CharacterCreation { Name = "Bob" }.Create();
        var q = c.Quests.Offer(QuestLibrary.SlashingPractice());
        c.Quests.Accept(q.Id);
        c.LearnAbility(AbilityLibrary.Slash);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse(AbilityLibrary.Slash);
        Assert.Equal(QuestState.ReadyToTurnIn, q.State);
        var back = SaveGame.Capture(c, MapIds.Forest, 1, 1).ToCharacter();
        Assert.Equal(QuestState.ReadyToTurnIn, back.Quests.Get(q.Id).State);
        Assert.True(back.Quests.TurnIn(q.Id));   // and the turn-in still works after loading
    }

    [Fact]
    public void CompletedQuestsDoNotPayAgainAfterLoading()
    {
        var c = new CharacterCreation { Name = "Bob" }.Create();
        var q = c.Quests.Offer(QuestLibrary.EnergyBallPractice());
        c.Quests.Accept(q.Id);
        c.LearnAbility(AbilityLibrary.EnergyBall);
        for (int i = 0; i < 5; i++) c.Quests.RecordAbilityUse(AbilityLibrary.EnergyBall);
        c.Quests.TurnIn(q.Id);
        var back = SaveGame.Capture(c, MapIds.Forest, 1, 1).ToCharacter();
        Assert.Equal(QuestState.Complete, back.Quests.Get(q.Id).State);
        Assert.Equal(c.Copper, back.Copper);
        Assert.Equal(c.Xp, back.Xp);
    }

    // ---- Save points ----

    private static TileMap Map(string name) =>
        TileMap.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../../src/WarInLocked.Game/Assets/Maps/" + name)));

    [Fact]
    public void SavePointIdsBelongToTheirMaps()
    {
        Assert.Equal("save-colonel", SavePoints.IdForMap(MapIds.Forest));
        Assert.Equal("save-whiplash", SavePoints.IdForMap(MapIds.AdventurersEncampment));
        Assert.Null(SavePoints.IdForMap(MapIds.Path));
        Assert.Equal(MapIds.Forest, SavePoints.MapOf("save-colonel"));
        Assert.Equal(MapIds.AdventurersEncampment, SavePoints.MapOf("save-whiplash"));
        Assert.Null(SavePoints.MapOf("nope"));
    }

    [Fact]
    public void FindNearUsesTheMapsSaveTilesOnAndAround()
    {
        var forest = Map("forest-of-beginnings.txt");
        var sp = Assert.Single(forest.SavePoints);
        Assert.Equal("save-colonel", SavePoints.FindNear(MapIds.Forest, forest, sp.X, sp.Y));
        Assert.Equal("save-colonel", SavePoints.FindNear(MapIds.Forest, forest, sp.X + 1, sp.Y + 1));
        Assert.Null(SavePoints.FindNear(MapIds.Forest, forest, sp.X + 3, sp.Y));
        var camp = Map("adventurers-encampment.txt");
        var cp = Assert.Single(camp.SavePoints);
        Assert.Equal("save-whiplash", SavePoints.FindNear(MapIds.AdventurersEncampment, camp, cp.X - 1, cp.Y));
        Assert.Null(SavePoints.FindNear(MapIds.Path, camp, cp.X, cp.Y));   // a map without save points
    }

    [Fact]
    public void InteractingSavesAndReportsIt()
    {
        var c = Rich();
        c.Combat.Update(100);   // out of combat
        Assert.False(c.InCombat);
        Assert.True(SavePoints.Interact(c, MapIds.AdventurersEncampment, 22, 8, out var notice, File1));
        Assert.Equal("Game saved", notice);
        var s = SaveGame.Load(File1);
        Assert.Equal(MapIds.AdventurersEncampment, s.MapId);
        Assert.Equal(22, s.TileX);
        Assert.Equal(8, s.TileY);
    }

    [Fact]
    public void CannotSaveInCombatOrWhileFallen()
    {
        var c = Rich();
        c.TakeDamage(1);   // in combat
        Assert.True(c.InCombat);
        Assert.False(SavePoints.Interact(c, MapIds.Forest, 1, 1, out var n1, File1));
        Assert.Equal("You cannot save in combat", n1);
        c.TakeDamage(10000);
        Assert.False(SavePoints.Interact(c, MapIds.Forest, 1, 1, out var n2, File1));
        Assert.Equal("You cannot save while fallen", n2);
        Assert.False(File.Exists(File1));
    }

    [Fact]
    public void DefaultPathIsUnderTheAppDataFolder()
    {
        Assert.EndsWith(Path.Combine("WarInLocked", "save.json"), SaveGame.DefaultPath);
        Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SaveGame.DefaultPath);
    }
}
