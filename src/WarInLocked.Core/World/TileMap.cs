using System;
using System.Collections.Generic;

namespace WarInLocked.Core.World
{
    public enum Tile { Floor, Grass, Tree, Wall, Water, Path, Fence, Tent, Bed, SavePoint }

    /// <summary>A placed thing read from the map text: player start, NPC, or slime spawn.</summary>
    public readonly struct MapMarker
    {
        public MapMarker(char symbol, int x, int y) { Symbol = symbol; X = x; Y = y; }
        public char Symbol { get; }
        public int X { get; }
        public int Y { get; }
    }

    /// <summary>
    /// A grid map built from text, one character per tile:
    /// # wall, T tree, . floor, g grass, = path, ~ water,
    /// P player start, N NPC, s slime spawn (markers stand on grass).
    /// Added for multi-map support (#53): F fence and X tent (both block walking; scenery only), and the door markers
    /// < > ^ v, which stand on a walkable tile on the map edge and lead out through that edge (west, east, north, south).
    /// Added for #54/#55 prep: b bed (scenery, blocks walking) and S save point (walkable marker, listed in SavePoints; no behaviour yet).
    /// </summary>
    public sealed class TileMap
    {
        private readonly Tile[,] _tiles;

        public int Width { get; }
        public int Height { get; }
        public (int X, int Y) PlayerStart { get; }
        public IReadOnlyList<MapMarker> Npcs { get; }
        public IReadOnlyList<MapMarker> SlimeSpawns { get; }
        /// <summary>Doors on the map edge: symbol '<' (west), '>' (east), '^' (north) or 'v' (south). Where they lead is the game's business.</summary>
        public IReadOnlyList<MapMarker> Doors { get; }
        /// <summary>Save point tiles ('S'). They are walkable; saving itself is not implemented yet.</summary>
        public IReadOnlyList<MapMarker> SavePoints { get; }

        private TileMap(Tile[,] tiles, (int, int) start, List<MapMarker> npcs, List<MapMarker> slimes, List<MapMarker> doors, List<MapMarker> savePoints)
        {
            _tiles = tiles;
            Width = tiles.GetLength(0);
            Height = tiles.GetLength(1);
            PlayerStart = start;
            Npcs = npcs;
            SlimeSpawns = slimes;
            Doors = doors;
            SavePoints = savePoints;
        }

        public Tile this[int x, int y] => InBounds(x, y) ? _tiles[x, y] : Tile.Wall;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsWalkable(int x, int y)
        {
            var t = this[x, y];
            return t == Tile.Floor || t == Tile.Grass || t == Tile.Path || t == Tile.SavePoint;
        }

        public static TileMap Parse(string text)
        {
            var lines = new List<string>();
            foreach (var raw in text.Replace("\r", "").Split('\n'))
                if (raw.Length > 0) lines.Add(raw);
            if (lines.Count == 0) throw new FormatException("Map is empty.");

            int width = 0;
            foreach (var l in lines) width = Math.Max(width, l.Length);
            var tiles = new Tile[width, lines.Count];
            (int, int)? start = null;
            var npcs = new List<MapMarker>();
            var slimes = new List<MapMarker>();
            var doors = new List<MapMarker>();
            var savePoints = new List<MapMarker>();

            for (int y = 0; y < lines.Count; y++)
                for (int x = 0; x < width; x++)
                {
                    char c = x < lines[y].Length ? lines[y][x] : '#';
                    switch (c)
                    {
                        case '#': tiles[x, y] = Tile.Wall; break;
                        case 'T': tiles[x, y] = Tile.Tree; break;
                        case '.': tiles[x, y] = Tile.Floor; break;
                        case 'g': tiles[x, y] = Tile.Grass; break;
                        case '=': tiles[x, y] = Tile.Path; break;
                        case '~': tiles[x, y] = Tile.Water; break;
                        case 'F': tiles[x, y] = Tile.Fence; break;
                        case 'X': tiles[x, y] = Tile.Tent; break;
                        case 'b': tiles[x, y] = Tile.Bed; break;
                        case 'S': tiles[x, y] = Tile.SavePoint; savePoints.Add(new MapMarker(c, x, y)); break;
                        case '<': case '>': case '^': case 'v': tiles[x, y] = Tile.Path; doors.Add(new MapMarker(c, x, y)); break;
                        case 'P': tiles[x, y] = Tile.Grass; start = (x, y); break;
                        case 'N': tiles[x, y] = Tile.Grass; npcs.Add(new MapMarker(c, x, y)); break;
                        case 's': tiles[x, y] = Tile.Grass; slimes.Add(new MapMarker(c, x, y)); break;
                        default: throw new FormatException($"Unknown map symbol '{c}' at {x},{y}.");
                    }
                }

            if (start == null) throw new FormatException("Map has no player start (P).");
            return new TileMap(tiles, start.Value, npcs, slimes, doors, savePoints);
        }

        public bool IsSavePoint(int x, int y) => this[x, y] == Tile.SavePoint;

        /// <summary>The door at this tile, or null.</summary>
        public MapMarker? DoorAt(int x, int y)
        {
            foreach (var d in Doors) if (d.X == x && d.Y == y) return d;
            return null;
        }
    }
}
