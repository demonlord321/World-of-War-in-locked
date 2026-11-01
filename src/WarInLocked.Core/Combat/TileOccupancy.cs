using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Combat
{
    /// <summary>
    /// Collision between the player and enemies on the tile grid (one body per tile). Core only decides; the screen asks before it moves.
    /// Player moving: <c>if (TileOccupancy.IsBlocked(x, y, enemies)) don't move</c>.
    /// Enemy moving: <c>TileOccupancy.TryMoveEnemy(enemy, x, y, playerX, playerY, enemies)</c> (or <see cref="IsBlockedForEnemy"/>).
    /// Dead enemies never block. Walls, NPCs and map bounds are the map's business, not this class's.
    /// </summary>
    public static class TileOccupancy
    {
        /// <summary>The living enemy standing on a tile, or null.</summary>
        public static Enemy EnemyAt(int x, int y, IEnumerable<Enemy> enemies)
        {
            if (enemies == null) return null;
            foreach (var e in enemies)
                if (e != null && !e.IsDead && e.TileX == x && e.TileY == y) return e;
            return null;
        }

        /// <summary>True when a living enemy holds the tile: the player must not move onto it.</summary>
        public static bool IsBlocked(int x, int y, IEnumerable<Enemy> enemies) => EnemyAt(x, y, enemies) != null;

        /// <summary>True when <paramref name="mover"/> may not step onto the tile: the player is on it or another living enemy holds it.
        /// The mover's own tile does not count against itself.</summary>
        public static bool IsBlockedForEnemy(Enemy mover, int x, int y, int playerX, int playerY, IEnumerable<Enemy> enemies)
        {
            if (mover == null) throw new ArgumentNullException(nameof(mover));
            if (x == playerX && y == playerY) return true;
            if (enemies == null) return false;
            foreach (var e in enemies)
                if (e != null && e != mover && !e.IsDead && e.TileX == x && e.TileY == y) return true;
            return false;
        }

        /// <summary>Moves the enemy to the tile unless <see cref="IsBlockedForEnemy"/>; returns whether it moved. A dead enemy never moves.</summary>
        public static bool TryMoveEnemy(Enemy mover, int x, int y, int playerX, int playerY, IEnumerable<Enemy> enemies)
        {
            if (mover == null) throw new ArgumentNullException(nameof(mover));
            if (mover.IsDead || IsBlockedForEnemy(mover, x, y, playerX, playerY, enemies)) return false;
            mover.TileX = x;
            mover.TileY = y;
            return true;
        }
    }
}
