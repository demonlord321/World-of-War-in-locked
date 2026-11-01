using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Saving
{
    /// <summary>A save file could not be read: missing, damaged, or from a newer game version. <see cref="Exception.Message"/> is a short line for the player.</summary>
    public sealed class SaveGameException : Exception
    {
        public SaveGameException(string message, Exception inner = null) : base(message, inner) { }
    }

    public sealed class SavedItemStack
    {
        public int Slot { get; set; }
        public string ItemId { get; set; }
        public int Count { get; set; }
    }

    public sealed class SavedEquipment
    {
        public EquipmentSlot Slot { get; set; }
        public string ItemId { get; set; }
    }

    public sealed class SavedHotbarSlot
    {
        public int Slot { get; set; }
        /// <summary>An ability id, or null.</summary>
        public string AbilityId { get; set; }
        /// <summary>A consumable item id, or null.</summary>
        public string ItemId { get; set; }
    }

    public sealed class SavedAbility
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public int Uses { get; set; }
        public int UsesIntoLevel { get; set; }
    }

    public sealed class SavedQuest
    {
        public string Id { get; set; }
        public QuestState State { get; set; }
        /// <summary>The progress (Current) of each objective, in the quest's objective order.</summary>
        public List<int> Progress { get; set; } = new List<int>();
    }

    /// <summary>
    /// The whole save (#54): one plain object that is written as JSON (System.Text.Json). <see cref="Capture"/> takes a snapshot of a
    /// <see cref="Character"/> plus where it stands, <see cref="ToCharacter"/> rebuilds it. <see cref="Save"/>/<see cref="Load"/> do the file.
    /// Not saved (by design, placeholder): cooldowns, the combat window, live slimes, the fallen countdown and the toast text.
    /// </summary>
    public sealed class SaveGame
    {
        /// <summary>The version written into new saves. A save with a higher version is refused; lower ones are read (nothing older exists yet).</summary>
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;

        // Who
        public string Name { get; set; }
        public Race Race { get; set; }
        public StatDistribution Distribution { get; set; }
        /// <summary>The eight stats now (creation values plus assigned points), in <see cref="StatType"/> order.</summary>
        public List<int> Stats { get; set; } = new List<int>();
        /// <summary>Points put into each stat so far, in <see cref="StatType"/> order.</summary>
        public List<int> AssignedPoints { get; set; } = new List<int>();
        public int FreeStatPoints { get; set; }
        public int Level { get; set; } = 1;
        /// <summary>Total XP, as <c>Character.Xp</c>.</summary>
        public int Xp { get; set; }
        public int Health { get; set; }
        public double Energy { get; set; }
        public int Copper { get; set; }
        /// <summary>Death debuff level (0-3, #35) and the seconds left on it. Absent in older saves = none.</summary>
        public int DeathDebuffLevel { get; set; }
        public double DeathDebuffRemaining { get; set; }

        // Things
        public List<SavedItemStack> Inventory { get; set; } = new List<SavedItemStack>();
        public int InventoryCapacity { get; set; } = Items.Inventory.DefaultCapacity;
        public List<SavedEquipment> Equipment { get; set; } = new List<SavedEquipment>();
        public List<SavedHotbarSlot> Hotbar { get; set; } = new List<SavedHotbarSlot>();
        public List<SavedAbility> Abilities { get; set; } = new List<SavedAbility>();
        public List<SavedQuest> Quests { get; set; } = new List<SavedQuest>();
        public List<string> Titles { get; set; } = new List<string>();
        public string EquippedTitle { get; set; }
        public List<string> Flags { get; set; } = new List<string>();

        // Where and when
        public string BindMapId { get; set; }
        public int BindTileX { get; set; }
        public int BindTileY { get; set; }
        /// <summary>The game clock as total game minutes since midnight of day 1 (<see cref="GameClock.TotalMinutes"/>).</summary>
        public double ClockMinutes { get; set; }
        /// <summary>The map the player stood on, e.g. "forest-of-beginnings".</summary>
        public string MapId { get; set; }
        /// <summary>The tile the player stood on.</summary>
        public int TileX { get; set; }
        public int TileY { get; set; }

        /// <summary>When it was saved (UTC), for a save list. Not read back into the game.</summary>
        public DateTime SavedAtUtc { get; set; }

        // ---- Capture / restore ----

        /// <summary>Takes a snapshot of the character and where it stands (map id and tile).</summary>
        public static SaveGame Capture(Character c, string mapId, int tileX, int tileY)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (string.IsNullOrWhiteSpace(mapId)) throw new ArgumentException("A save needs a map id.", nameof(mapId));
            var s = new SaveGame
            {
                Name = c.Name, Race = c.Race, Distribution = c.Distribution,
                FreeStatPoints = c.FreeStatPoints, Level = c.Level, Xp = c.Xp,
                Health = c.Health, Energy = c.EnergyExact, Copper = c.Copper,
                DeathDebuffLevel = c.DeathDebuffLevel, DeathDebuffRemaining = c.DeathDebuffRemaining,
                InventoryCapacity = c.Inventory.Capacity,
                EquippedTitle = c.EquippedTitle?.Id,
                BindMapId = c.BindPoint.MapId, BindTileX = c.BindPoint.TileX, BindTileY = c.BindPoint.TileY,
                ClockMinutes = c.Clock.TotalMinutes,
                MapId = mapId, TileX = tileX, TileY = tileY,
                SavedAtUtc = DateTime.UtcNow,
            };
            foreach (var stat in StatTypes.All)
            {
                s.Stats.Add(c.Stats[stat]);
                s.AssignedPoints.Add(c.AssignedPoints(stat));
            }
            for (int i = 0; i < c.Inventory.Capacity; i++)
            {
                var stack = c.Inventory.Get(i);
                if (stack != null) s.Inventory.Add(new SavedItemStack { Slot = i, ItemId = stack.Item.Id, Count = stack.Count });
            }
            foreach (var slot in EquipmentSlots.All)
            {
                var item = c.Equipment.Get(slot);
                if (item != null) s.Equipment.Add(new SavedEquipment { Slot = slot, ItemId = item.Id });
            }
            for (int i = 0; i < Combat.Hotbar.SlotCount; i++)
            {
                var slot = c.Combat.Hotbar[i];
                if (!slot.IsEmpty) s.Hotbar.Add(new SavedHotbarSlot { Slot = i, AbilityId = slot.AbilityId, ItemId = slot.ItemId });
            }
            foreach (var id in c.Abilities)
            {
                var a = c.GetAbility(id);
                s.Abilities.Add(a == null
                    ? new SavedAbility { Id = id, Level = 1 }
                    : new SavedAbility { Id = id, Level = a.Level, Uses = a.Uses, UsesIntoLevel = a.UsesIntoLevel });
            }
            foreach (var q in c.Quests.All)
            {
                var sq = new SavedQuest { Id = q.Id, State = q.State };
                foreach (var o in q.Objectives) sq.Progress.Add(o.Current);
                s.Quests.Add(sq);
            }
            foreach (var t in c.Titles) s.Titles.Add(t.Id);
            s.Flags.AddRange(c.Flags);
            return s;
        }

        /// <summary>
        /// Builds a new <see cref="Character"/> from the save. The game then moves the player to <see cref="MapId"/> / <see cref="TileX"/>, <see cref="TileY"/>.
        /// Ids the game no longer knows (items, quests, titles) are skipped. A save made while fallen comes back with 1 health.
        /// </summary>
        public Character ToCharacter()
        {
            if (Version > CurrentVersion) throw new SaveGameException("This save is from a newer version of the game.");
            if (string.IsNullOrWhiteSpace(Name) || Stats == null || Stats.Count != StatTypes.Count)
                throw new SaveGameException("This save is damaged.");

            var stats = new StatBlock(Stats[0], Stats[1], Stats[2], Stats[3], Stats[4], Stats[5], Stats[6], Stats[7]);
            var c = new Character(Name, Race, Distribution, stats);

            c.RestoreTitles(Titles ?? new List<string>(), EquippedTitle);
            c.RestoreFlags(Flags ?? new List<string>());
            c.RestoreDeathDebuff(DeathDebuffLevel, DeathDebuffRemaining);   // before the progress: health is capped at the reduced maximum
            c.RestoreProgress(Level, Xp, FreeStatPoints, AssignedPoints, Health, Energy, Copper);

            if (Inventory != null)
                foreach (var st in Inventory)
                {
                    var item = ItemLibrary.Get(st.ItemId);
                    if (item == null || st.Slot < 0 || st.Slot >= c.Inventory.Capacity || st.Count < 1) continue;
                    c.Inventory.Replace(st.Slot, new ItemStack(item, Math.Min(st.Count, item.MaxStack)));
                }
            if (Equipment != null)
                foreach (var e in Equipment)
                {
                    var item = ItemLibrary.Get(e.ItemId);
                    if (item != null) c.Equipment.Restore(e.Slot, item);
                }
            if (Abilities != null)
                foreach (var a in Abilities) c.RestoreAbility(a.Id, a.Level, a.Uses, a.UsesIntoLevel);
            if (Hotbar != null)
                foreach (var h in Hotbar)
                {
                    if (h.Slot < 0 || h.Slot >= Combat.Hotbar.SlotCount) continue;
                    if (h.AbilityId != null && new List<string>(c.Abilities).Contains(h.AbilityId)) c.Combat.Hotbar.Assign(h.Slot, h.AbilityId);
                    else if (h.ItemId != null && ItemLibrary.Get(h.ItemId) != null) c.Combat.Hotbar.AssignItem(h.Slot, h.ItemId);
                }
            if (Quests != null)
                foreach (var sq in Quests)
                {
                    var quest = QuestLibrary.Create(sq.Id);
                    if (quest == null) continue;
                    c.Quests.Offer(quest);
                    quest.State = sq.State;
                    for (int i = 0; i < quest.Objectives.Count && sq.Progress != null && i < sq.Progress.Count; i++)
                        quest.Objectives[i].Current = Math.Max(0, Math.Min(sq.Progress[i], quest.Objectives[i].Required));
                }
            if (!string.IsNullOrWhiteSpace(BindMapId) && BindTileX >= 0 && BindTileY >= 0)
                c.SetBindPoint(new BindPoint(BindMapId, BindTileX, BindTileY));
            c.Clock.SetTotalMinutes(ClockMinutes);
            return c;
        }

        // ---- JSON ----

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public string ToJson() => JsonSerializer.Serialize(this, Options);

        /// <summary>Reads a save from JSON. Throws <see cref="SaveGameException"/> for damaged text or a newer version.</summary>
        public static SaveGame FromJson(string json)
        {
            SaveGame s;
            try
            {
                s = JsonSerializer.Deserialize<SaveGame>(json, Options);
            }
            catch (Exception e) when (e is JsonException || e is ArgumentException || e is NotSupportedException || e is InvalidOperationException)
            {
                throw new SaveGameException("This save is damaged.", e);
            }
            if (s == null) throw new SaveGameException("This save is damaged.");
            if (s.Version > CurrentVersion) throw new SaveGameException("This save is from a newer version of the game.");
            if (s.Version < 1) throw new SaveGameException("This save is damaged.");
            if (string.IsNullOrWhiteSpace(s.Name) || s.Stats == null || s.Stats.Count != StatTypes.Count) throw new SaveGameException("This save is damaged.");
            return s;
        }

        // ---- Files ----

        /// <summary>
        /// PLACEHOLDER default folder: <c>WarInLocked</c> under the user's application data folder (Windows %APPDATA%, Linux ~/.config).
        /// </summary>
        public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WarInLocked");

        /// <summary>PLACEHOLDER default file: one save slot, <c>save.json</c> in <see cref="DefaultDirectory"/>.</summary>
        public static string DefaultPath => Path.Combine(DefaultDirectory, "save.json");

        /// <summary>True when a file exists at the path (default: <see cref="DefaultPath"/>). Enables the main menu's "Load game".</summary>
        public static bool Exists(string path = null) => File.Exists(path ?? DefaultPath);

        /// <summary>Writes the save (creating the folder). Writes a temporary file first, so a crash cannot leave half a save. Overwrites the old save.</summary>
        public void Save(string path = null)
        {
            path = path ?? DefaultPath;
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, ToJson());
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>Snapshots and writes in one go.</summary>
        public static SaveGame Save(Character c, string mapId, int tileX, int tileY, string path = null)
        {
            var s = Capture(c, mapId, tileX, tileY);
            s.Save(path);
            return s;
        }

        /// <summary>Reads the save file. Throws <see cref="SaveGameException"/> (message for the player) when it is missing, damaged or from a newer version.</summary>
        public static SaveGame Load(string path = null)
        {
            path = path ?? DefaultPath;
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                throw new SaveGameException("No save found.", e);
            }
            return FromJson(json);
        }

        /// <summary>Like <see cref="Load"/> but returns false (with the message in <paramref name="error"/>) instead of throwing.</summary>
        public static bool TryLoad(string path, out SaveGame save, out string error)
        {
            try
            {
                save = Load(path);
                error = null;
                return true;
            }
            catch (SaveGameException e)
            {
                save = null;
                error = e.Message;
                return false;
            }
        }
    }
}
