using WarInLocked.Core.Characters;
using WarInLocked.Core.Saving;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#42 one autosave slot plus three manual slots.</summary>
public class SaveSlotsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wil-slots-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private static Character Hero(string name = "Aragorn", int levels = 0)
    {
        var c = new CharacterCreation { Name = name, Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        if (levels > 0) c.GainXp((int)Leveling.TotalXpFor(1 + levels));
        return c;
    }

    [Fact]
    public void FourSlotsAutoFirstThenThreeManual()
    {
        Assert.Equal(new[] { SaveSlot.Auto, SaveSlot.Manual1, SaveSlot.Manual2, SaveSlot.Manual3 }, SaveSlots.All.ToArray());
        Assert.Equal(3, SaveSlots.ManualSlotCount);
        Assert.Equal(new[] { "Auto save", "Save 1", "Save 2", "Save 3" }, SaveSlots.All.Select(SaveSlots.LabelOf).ToArray());
        Assert.False(SaveSlots.IsManual(SaveSlot.Auto));
        Assert.All(SaveSlots.Manual, s => Assert.True(SaveSlots.IsManual(s)));
        Assert.Equal(4, SaveSlots.All.Select(s => SaveSlots.PathOf(s, _dir)).Distinct().Count());
    }

    [Fact]
    public void EmptyListShowsFourEmptyRows()
    {
        var list = SaveSlots.List(_dir);
        Assert.Equal(4, list.Count);
        Assert.All(list, i => { Assert.True(i.IsEmpty); Assert.False(i.CanLoad); Assert.Null(i.Save); });
        Assert.False(SaveSlots.AnyExists(_dir));
        Assert.Null(SaveSlots.Latest(_dir));
    }

    [Fact]
    public void SlotsAreIndependent()
    {
        SaveSlots.Save(SaveSlot.Manual1, Hero("One"), MapIds.Forest, 3, 4, _dir);
        SaveSlots.Save(SaveSlot.Manual3, Hero("Three", 2), MapIds.Forest, 5, 6, _dir);

        Assert.True(SaveSlots.AnyExists(_dir));
        var list = SaveSlots.List(_dir);
        Assert.True(list[0].IsEmpty);
        Assert.Equal("One", list[1].Save.Name);
        Assert.True(list[2].IsEmpty);
        Assert.Equal("Three", list[3].Save.Name);
        Assert.Equal(3, list[3].Save.Level);
        Assert.Equal(5, SaveSlots.Load(SaveSlot.Manual3, _dir).TileX);
        Assert.Equal("Three", SaveSlots.Load(SaveSlot.Manual3, _dir).ToCharacter().Name);
    }

    [Fact]
    public void SavingAgainOverwritesJustThatSlot()
    {
        SaveSlots.Save(SaveSlot.Manual2, Hero("Old"), MapIds.Forest, 1, 1, _dir);
        SaveSlots.Save(SaveSlot.Manual2, Hero("New"), MapIds.Forest, 2, 2, _dir);
        Assert.Equal("New", SaveSlots.Load(SaveSlot.Manual2, _dir).Name);
    }

    [Fact]
    public void AutosaveWritesTheAutoSlotOnly()
    {
        Assert.True(SaveSlots.Autosave(Hero("Auto"), MapIds.Forest, 7, 8, out var error, _dir));
        Assert.Null(error);
        Assert.Equal("Auto", SaveSlots.Info(SaveSlot.Auto, _dir).Save.Name);
        Assert.True(SaveSlots.Info(SaveSlot.Auto, _dir).IsAuto);
        Assert.All(SaveSlots.Manual, s => Assert.False(SaveSlots.Exists(s, _dir)));
    }

    [Fact]
    public void AutosaveIsSkippedWhileFallen()
    {
        var c = Hero();
        c.TakeDamage(c.MaxHealth + 1);
        Assert.False(SaveSlots.Autosave(c, MapIds.Forest, 1, 1, out var error, _dir));
        Assert.Equal(SavePoints.FallenNotice, error);
        Assert.False(SaveSlots.Exists(SaveSlot.Auto, _dir));
    }

    [Fact]
    public void ADamagedSlotIsListedButCannotBeLoaded()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SaveSlots.PathOf(SaveSlot.Manual2, _dir), "{ not json");
        var info = SaveSlots.Info(SaveSlot.Manual2, _dir);
        Assert.True(info.Exists);
        Assert.False(info.CanLoad);
        Assert.NotNull(info.Error);
        Assert.Throws<SaveGameException>(() => SaveSlots.Load(SaveSlot.Manual2, _dir));
        Assert.False(SaveSlots.TryLoad(SaveSlot.Manual2, out _, out _, _dir));
    }

    [Fact]
    public void LoadOfAnEmptySlotThrows()
    {
        Assert.Throws<SaveGameException>(() => SaveSlots.Load(SaveSlot.Manual1, _dir));
    }

    [Fact]
    public void LatestIsTheMostRecentlyWrittenLoadableSlot()
    {
        SaveSlots.Save(SaveSlot.Manual1, Hero("First"), MapIds.Forest, 1, 1, _dir);
        System.Threading.Thread.Sleep(20);
        SaveSlots.Autosave(Hero("Second"), MapIds.Forest, 1, 1, out _, _dir);
        Assert.Equal(SaveSlot.Auto, SaveSlots.Latest(_dir).Slot);
        System.Threading.Thread.Sleep(20);
        SaveSlots.Save(SaveSlot.Manual3, Hero("Third"), MapIds.Forest, 1, 1, _dir);
        Assert.Equal(SaveSlot.Manual3, SaveSlots.Latest(_dir).Slot);
    }

    [Fact]
    public void DeleteEmptiesASlot()
    {
        SaveSlots.Save(SaveSlot.Manual1, Hero(), MapIds.Forest, 1, 1, _dir);
        Assert.True(SaveSlots.Delete(SaveSlot.Manual1, _dir));
        Assert.False(SaveSlots.Delete(SaveSlot.Manual1, _dir));
        Assert.True(SaveSlots.Info(SaveSlot.Manual1, _dir).IsEmpty);
    }

    [Fact]
    public void TheOldSingleSaveBecomesSaveOne()
    {
        Directory.CreateDirectory(_dir);
        SaveGame.Save(Hero("Legacy"), MapIds.Forest, 1, 1, Path.Combine(_dir, "save.json"));
        Assert.True(SaveSlots.AnyExists(_dir));
        Assert.Equal("Legacy", SaveSlots.Info(SaveSlot.Manual1, _dir).Save.Name);
        Assert.False(File.Exists(Path.Combine(_dir, "save.json")));
    }

    [Fact]
    public void TheOldSaveDoesNotReplaceAnExistingSaveOne()
    {
        Directory.CreateDirectory(_dir);
        SaveSlots.Save(SaveSlot.Manual1, Hero("Keep"), MapIds.Forest, 1, 1, _dir);
        SaveGame.Save(Hero("Legacy"), MapIds.Forest, 1, 1, Path.Combine(_dir, "save.json"));
        Assert.Equal("Keep", SaveSlots.Info(SaveSlot.Manual1, _dir).Save.Name);
        Assert.True(File.Exists(Path.Combine(_dir, "save.json")));
    }

    [Fact]
    public void ASavePointWritesTheChosenManualSlot()
    {
        var c = Hero("Saver");
        c.Combat.Update(100);   // out of combat
        Assert.True(SavePoints.Interact(c, MapIds.Forest, 1, 1, SaveSlot.Manual2, out var notice, _dir));
        Assert.Equal(SavePoints.SavedNotice, notice);
        Assert.Equal("Saver", SaveSlots.Info(SaveSlot.Manual2, _dir).Save.Name);
        Assert.True(SaveSlots.Info(SaveSlot.Manual1, _dir).IsEmpty);
    }

    [Fact]
    public void ASavePointRefusesTheAutosaveSlot()
    {
        Assert.Throws<ArgumentException>(() => SavePoints.Interact(Hero(), MapIds.Forest, 1, 1, SaveSlot.Auto, out _, _dir));
    }
}
