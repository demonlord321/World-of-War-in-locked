using System.Collections.Generic;

namespace WarInLocked.Core.World
{
    /// <summary>
    /// The map ids and the per-map "safe zone" flag (#57). Dyllan: "Monsters cannot enter safe zones. Will be established later 'Non combat zones'
    /// and Wild zones." A safe zone is a non-combat zone (inns, camps); every other map is a wild zone. The ids are the ones the Game uses for its map files.
    /// </summary>
    public static class MapIds
    {
        public const string Forest = BindPoint.ForestOfBeginningsMapId;
        public const string Path = "path";
        public const string AdventurersEncampment = "adventurers-encampment";

        // PLACEHOLDER: only the encampment is a safe zone for now. The forest and the path are wild zones.
        private static readonly HashSet<string> Safe = new HashSet<string> { AdventurersEncampment };

        /// <summary>True for a safe (non-combat) zone: monsters cannot enter it (nothing chases yet, so for now the map just must not have slime spawns). Unknown ids are wild.</summary>
        public static bool IsSafeZone(string mapId) => mapId != null && Safe.Contains(mapId);

        /// <summary>Whether monsters may be on this map: the opposite of <see cref="IsSafeZone"/>. A spawner/chaser asks this before placing or moving a monster onto a map.</summary>
        public static bool AllowsMonsters(string mapId) => !IsSafeZone(mapId);
    }
}
