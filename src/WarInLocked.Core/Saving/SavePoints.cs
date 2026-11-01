using System;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Interaction;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Saving
{
    /// <summary>
    /// The save points (#54): "for now place two save points, one near Colonel Powers, the other near Whiplash". The tiles are Mapmaker's ('S' in the map files,
    /// <c>TileMap.SavePoints</c>); this class gives each one its id and does the interaction: stand on the tile or next to it and press the talk key.
    /// </summary>
    public static class SavePoints
    {
        public const string ColonelId = "save-colonel";
        public const string WhiplashId = "save-whiplash";

        public const string SavedNotice = "Game saved";   // PLACEHOLDER
        public const string FallenNotice = "You cannot save while fallen";   // PLACEHOLDER
        public const string CombatNotice = "You cannot save in combat";   // PLACEHOLDER
        public const string FailedNotice = "Could not save the game";   // PLACEHOLDER

        /// <summary>The save point id of a map (forest -> save-colonel, encampment -> save-whiplash), or null when the map has none.</summary>
        public static string IdForMap(string mapId)
        {
            switch (mapId)
            {
                case MapIds.Forest: return ColonelId;
                case MapIds.AdventurersEncampment: return WhiplashId;
                default: return null;
            }
        }

        /// <summary>The map a save point id belongs to, or null.</summary>
        public static string MapOf(string savePointId)
        {
            switch (savePointId)
            {
                case ColonelId: return MapIds.Forest;
                case WhiplashId: return MapIds.AdventurersEncampment;
                default: return null;
            }
        }

        /// <summary>
        /// The id of the save point the player can use right now: on a save point tile or on a tile next to one (diagonals count, like talking),
        /// on a map that has save points. Null when none is within reach.
        /// </summary>
        public static string FindNear(string mapId, TileMap map, int playerX, int playerY)
        {
            string id = IdForMap(mapId);
            if (id == null || map == null) return null;
            foreach (var sp in map.SavePoints)
                if ((sp.X == playerX && sp.Y == playerY) || TalkTarget.IsAdjacent(playerX, playerY, sp.X, sp.Y)) return id;
            return null;
        }

        /// <summary>Why the game cannot be saved right now (fallen, in combat), or null.</summary>
        public static string CannotSaveReason(Character c) => c.IsDead ? FallenNotice : c.InCombat ? CombatNotice : null;

        /// <summary>
        /// Saves at a save point: refuses while fallen or in combat (PLACEHOLDER rules), else writes <see cref="SaveGame.Capture"/> to the path
        /// (default <see cref="SaveGame.DefaultPath"/>), replacing the old save. Returns true when saved; <paramref name="notice"/> is the message to show.
        /// </summary>
        public static bool Interact(Character c, string mapId, int playerX, int playerY, out string notice, string path = null)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            string why = CannotSaveReason(c);
            if (why != null) { notice = why; return false; }
            try
            {
                SaveGame.Save(c, mapId, playerX, playerY, path);
            }
            catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException)
            {
                notice = FailedNotice;
                return false;
            }
            notice = SavedNotice;
            return true;
        }
    }
}
