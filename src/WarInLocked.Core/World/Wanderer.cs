using System;

namespace WarInLocked.Core.World
{
    /// <summary>
    /// Something that ambles around the map one tile at a time (placeholder slimes).
    /// It pauses, picks a random walkable neighbour, glides there, and repeats.
    /// No combat yet: this is movement only.
    /// </summary>
    public sealed class Wanderer
    {
        private static readonly (int dx, int dy)[] Dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        private readonly Random _rng;
        private float _wait;
        private float _progress = 1f;

        public Wanderer(int x, int y, Random rng)
        {
            TileX = FromX = x;
            TileY = FromY = y;
            _rng = rng;
            _wait = (float)_rng.NextDouble() * 1.5f;
        }

        public int TileX { get; private set; }
        public int TileY { get; private set; }
        public int FromX { get; private set; }
        public int FromY { get; private set; }

        /// <summary>Tiles per second while gliding.</summary>
        public float Speed { get; set; } = 2.5f;

        /// <summary>Position in tiles, smoothly between the old and new tile.</summary>
        public float X => FromX + (TileX - FromX) * _progress;
        public float Y => FromY + (TileY - FromY) * _progress;

        /// <summary>Moves to a neighbouring (or any) tile chosen elsewhere, e.g. by <c>CombatController.UpdateEnemies</c> when the slime chases (#31); it glides there like a normal step.
        /// Does nothing if it is already on that tile.</summary>
        public void GlideTo(int x, int y)
        {
            if (x == TileX && y == TileY) return;
            FromX = (int)Math.Round(X); FromY = (int)Math.Round(Y);   // start from where it is drawn now
            TileX = x; TileY = y;
            _progress = 0f;
        }

        /// <summary>Advances only the glide (no wandering): call it instead of <see cref="Update"/> while the slime is chasing.</summary>
        public void Glide(float seconds)
        {
            if (_progress < 1f) _progress = Math.Min(1f, _progress + seconds * Speed);
        }

        /// <param name="blocked">Extra tiles to avoid, e.g. the player or other slimes.</param>
        public void Update(float seconds, TileMap map, Func<int, int, bool>? blocked = null)
        {
            if (_progress < 1f)
            {
                _progress = Math.Min(1f, _progress + seconds * Speed);
                return;
            }

            _wait -= seconds;
            if (_wait > 0) return;
            _wait = 0.6f + (float)_rng.NextDouble() * 1.4f;

            var (dx, dy) = Dirs[_rng.Next(Dirs.Length)];
            int nx = TileX + dx, ny = TileY + dy;
            if (!map.IsWalkable(nx, ny) || (blocked != null && blocked(nx, ny))) return;

            FromX = TileX; FromY = TileY;
            TileX = nx; TileY = ny;
            _progress = 0f;
        }
    }
}
