using System;

namespace WarInLocked.Core.World
{
    /// <summary>
    /// Where a fallen character comes back (#29): a map plus a tile on it. Only one is set at a time
    /// (<c>Character.SetBindPoint</c> replaces it). Set at inns and rest locations (none exist yet).
    /// </summary>
    public sealed class BindPoint : IEquatable<BindPoint>
    {
        /// <summary>Map id of the tutorial forest (the id the Game uses for forest-of-beginnings.txt).</summary>
        public const string ForestOfBeginningsMapId = "forest-of-beginnings";

        /// <summary>
        /// The default bind point: next to Colonel Powers in the Forest of Beginnings, on the free tile south of him
        /// (he stands at tile 22,12). A test checks this against the map file, so it follows the map.
        /// </summary>
        public static readonly BindPoint Tutorial = new BindPoint(ForestOfBeginningsMapId, 22, 13);

        public BindPoint(string mapId, int tileX, int tileY)
        {
            if (string.IsNullOrWhiteSpace(mapId)) throw new ArgumentException("A bind point needs a map id.", nameof(mapId));
            if (tileX < 0) throw new ArgumentOutOfRangeException(nameof(tileX));
            if (tileY < 0) throw new ArgumentOutOfRangeException(nameof(tileY));
            MapId = mapId;
            TileX = tileX;
            TileY = tileY;
        }

        public string MapId { get; }
        public int TileX { get; }
        public int TileY { get; }

        /// <summary>
        /// The first free walkable tile around an NPC, trying south, east, west, north, then the diagonals.
        /// A tile is free when it is walkable and holds no NPC or slime spawn. Throws if there is none.
        /// </summary>
        public static BindPoint NextToNpc(string mapId, TileMap map, int npcX, int npcY)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            int[,] around = { { 0, 1 }, { 1, 0 }, { -1, 0 }, { 0, -1 }, { 1, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 } };
            for (int i = 0; i < around.GetLength(0); i++)
            {
                int x = npcX + around[i, 0], y = npcY + around[i, 1];
                if (!map.IsWalkable(x, y)) continue;
                bool taken = false;
                foreach (var n in map.Npcs) if (n.X == x && n.Y == y) taken = true;
                foreach (var s in map.SlimeSpawns) if (s.X == x && s.Y == y) taken = true;
                if (!taken) return new BindPoint(mapId, x, y);
            }
            throw new InvalidOperationException("No free tile next to the NPC.");
        }

        public bool Equals(BindPoint other) => other != null && other.MapId == MapId && other.TileX == TileX && other.TileY == TileY;
        public override bool Equals(object obj) => Equals(obj as BindPoint);
        public override int GetHashCode() => (MapId.GetHashCode() * 31 + TileX) * 31 + TileY;
        public override string ToString() => MapId + " (" + TileX + ", " + TileY + ")";
    }
}
