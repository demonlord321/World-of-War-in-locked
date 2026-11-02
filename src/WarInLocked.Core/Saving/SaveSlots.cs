using System;
using System.Collections.Generic;
using System.IO;
using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Saving
{
    /// <summary>The four places a game can be saved (#42): one autosave on top, then three manual slots.</summary>
    public enum SaveSlot
    {
        Auto = 0,
        Manual1 = 1,
        Manual2 = 2,
        Manual3 = 3,
    }

    /// <summary>What a save list row shows for one slot.</summary>
    public sealed class SaveSlotInfo
    {
        internal SaveSlotInfo(SaveSlot slot, string label, bool exists, SaveGame save, string error)
        {
            Slot = slot; Label = label; Exists = exists; Save = save; Error = error;
        }

        public SaveSlot Slot { get; }
        /// <summary>"Auto save", "Save 1", "Save 2" or "Save 3".</summary>
        public string Label { get; }
        /// <summary>True when a file is there (it may still be damaged, see <see cref="Error"/>).</summary>
        public bool Exists { get; }
        /// <summary>The loaded save for the row text (name, level, map, saved time), or null when empty or damaged.</summary>
        public SaveGame Save { get; }
        /// <summary>The message when the file exists but cannot be read (damaged or from a newer version), else null. Show the row but make it unusable.</summary>
        public string Error { get; }
        public bool IsEmpty => !Exists;
        /// <summary>True when the slot can be loaded.</summary>
        public bool CanLoad => Exists && Save != null;
        public bool IsAuto => Slot == SaveSlot.Auto;
    }

    /// <summary>
    /// The save slots (#42). Dyllan: "If we can implement an auto save. Lets do that, for manual save slots let's say 3, the top one will be the auto save
    /// slot and there will only be one. So it will be Auto save, then three manual save slots" (corrected: "Let's do that"). Each slot is its own file in
    /// the save folder. When autosave happens is still open (see QUESTIONS.md): Core only gives <see cref="Autosave"/>; the Game decides when to call it.
    /// </summary>
    public static class SaveSlots
    {
        /// <summary>The slots in list order: Auto first, then the three manual ones.</summary>
        public static readonly IReadOnlyList<SaveSlot> All = new[] { SaveSlot.Auto, SaveSlot.Manual1, SaveSlot.Manual2, SaveSlot.Manual3 };
        /// <summary>The manual slots a player saves into on purpose.</summary>
        public static readonly IReadOnlyList<SaveSlot> Manual = new[] { SaveSlot.Manual1, SaveSlot.Manual2, SaveSlot.Manual3 };

        public const int ManualSlotCount = 3;

        public static string LabelOf(SaveSlot slot)
        {
            switch (slot)
            {
                case SaveSlot.Auto: return "Auto save";
                case SaveSlot.Manual1: return "Save 1";
                case SaveSlot.Manual2: return "Save 2";
                case SaveSlot.Manual3: return "Save 3";
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static bool IsManual(SaveSlot slot) => slot == SaveSlot.Manual1 || slot == SaveSlot.Manual2 || slot == SaveSlot.Manual3;

        private static string FileOf(SaveSlot slot)
        {
            switch (slot)
            {
                case SaveSlot.Auto: return "autosave.json";
                case SaveSlot.Manual1: return "save-1.json";
                case SaveSlot.Manual2: return "save-2.json";
                case SaveSlot.Manual3: return "save-3.json";
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        /// <summary>The file of a slot in <paramref name="directory"/> (default <see cref="SaveGame.DefaultDirectory"/>).</summary>
        public static string PathOf(SaveSlot slot, string directory = null) => Path.Combine(directory ?? SaveGame.DefaultDirectory, FileOf(slot));

        public static bool Exists(SaveSlot slot, string directory = null) => File.Exists(PathOf(slot, directory));

        /// <summary>True when any slot has a file: enables "Continue" / "Load game" on the main menu.</summary>
        public static bool AnyExists(string directory = null)
        {
            MigrateLegacy(directory);
            foreach (var s in All) if (Exists(s, directory)) return true;
            return false;
        }

        /// <summary>
        /// The first version had one file, <c>save.json</c>. If it is still there and manual slot 1 is empty it becomes Save 1 (the file is moved).
        /// Called by <see cref="AnyExists"/> and <see cref="Info"/>; safe to call again.
        /// </summary>
        public static void MigrateLegacy(string directory = null)
        {
            string dir = directory ?? SaveGame.DefaultDirectory;
            string legacy = Path.Combine(dir, "save.json");
            string target = PathOf(SaveSlot.Manual1, dir);
            try
            {
                if (File.Exists(legacy) && !File.Exists(target)) File.Move(legacy, target);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { /* leave it; the old file just is not listed */ }
        }

        /// <summary>One row of the save list.</summary>
        public static SaveSlotInfo Info(SaveSlot slot, string directory = null)
        {
            MigrateLegacy(directory);
            string path = PathOf(slot, directory);
            string label = LabelOf(slot);
            if (!File.Exists(path)) return new SaveSlotInfo(slot, label, false, null, null);
            return SaveGame.TryLoad(path, out var save, out var error)
                ? new SaveSlotInfo(slot, label, true, save, null)
                : new SaveSlotInfo(slot, label, true, null, error);
        }

        /// <summary>The whole list in display order: Auto save first, then Save 1 to 3.</summary>
        public static IReadOnlyList<SaveSlotInfo> List(string directory = null)
        {
            var r = new List<SaveSlotInfo>();
            foreach (var s in All) r.Add(Info(s, directory));
            return r;
        }

        /// <summary>The loadable slot that was written last (for a "Continue" button), or null when none can be loaded.</summary>
        public static SaveSlotInfo Latest(string directory = null)
        {
            SaveSlotInfo best = null;
            foreach (var i in List(directory))
                if (i.CanLoad && (best == null || i.Save.SavedAtUtc > best.Save.SavedAtUtc)) best = i;
            return best;
        }

        /// <summary>Writes the character into the slot, replacing what was there. Throws IO exceptions like <see cref="SaveGame.Save(Character,string,int,int,string)"/>.</summary>
        public static SaveGame Save(SaveSlot slot, Character c, string mapId, int tileX, int tileY, string directory = null) =>
            SaveGame.Save(c, mapId, tileX, tileY, PathOf(slot, directory));

        /// <summary>Reads a slot. Throws <see cref="SaveGameException"/> when empty, damaged or from a newer version.</summary>
        public static SaveGame Load(SaveSlot slot, string directory = null)
        {
            MigrateLegacy(directory);
            return SaveGame.Load(PathOf(slot, directory));
        }

        public static bool TryLoad(SaveSlot slot, out SaveGame save, out string error, string directory = null)
        {
            MigrateLegacy(directory);
            return SaveGame.TryLoad(PathOf(slot, directory), out save, out error);
        }

        /// <summary>Empties a slot. Returns true when a file was removed.</summary>
        public static bool Delete(SaveSlot slot, string directory = null)
        {
            string path = PathOf(slot, directory);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>
        /// Writes the one autosave slot (PLACEHOLDER rule: refused while fallen, in combat is fine). Returns true when written. Never throws for IO problems:
        /// an autosave that fails is skipped quietly (<paramref name="error"/> says why). WHEN to call it is the Game's choice and an open question
        /// (suggest: on changing map, after a level-up or quest hand-in).
        /// </summary>
        public static bool Autosave(Character c, string mapId, int tileX, int tileY, out string error, string directory = null)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            error = null;
            if (c.IsDead) { error = SavePoints.FallenNotice; return false; }
            try
            {
                Save(SaveSlot.Auto, c, mapId, tileX, tileY, directory);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                error = SavePoints.FailedNotice;
                return false;
            }
        }
    }
}
