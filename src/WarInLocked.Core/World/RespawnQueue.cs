using System;
using System.Collections.Generic;

namespace WarInLocked.Core.World
{
    /// <summary>A tile where something is due to come back.</summary>
    public sealed class RespawnPoint
    {
        public RespawnPoint(int tileX, int tileY) { TileX = tileX; TileY = tileY; }
        public int TileX { get; }
        public int TileY { get; }
    }

    /// <summary>
    /// Slimes come back (#19, so "use Slash 5 times" is doable): schedule a point when one dies, call <see cref="Update"/> every frame,
    /// and spawn a fresh one at each point it returns. If the tile is busy, <see cref="Schedule"/> it again with a short delay.
    /// </summary>
    public sealed class RespawnQueue
    {
        /// <summary>PLACEHOLDER: seconds until a defeated slime is replaced.</summary>
        public const double DefaultDelaySeconds = 10.0;

        private sealed class Entry { public RespawnPoint Point; public double Left; }
        private readonly List<Entry> _entries = new List<Entry>();

        public int Pending => _entries.Count;

        public void Schedule(int tileX, int tileY, double delaySeconds = DefaultDelaySeconds)
        {
            if (delaySeconds < 0) throw new ArgumentOutOfRangeException(nameof(delaySeconds));
            _entries.Add(new Entry { Point = new RespawnPoint(tileX, tileY), Left = delaySeconds });
        }

        /// <summary>Advances the timers and returns (and removes) the points that are due.</summary>
        public IReadOnlyList<RespawnPoint> Update(double seconds)
        {
            var due = new List<RespawnPoint>();
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                _entries[i].Left -= seconds;
                if (_entries[i].Left > 0) continue;
                due.Insert(0, _entries[i].Point);
                _entries.RemoveAt(i);
            }
            return due;
        }
    }
}
