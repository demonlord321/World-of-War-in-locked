using System;
using System.Collections.Generic;
using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Items
{
    /// <summary>Something lying on the ground of a map (#36, #62): a stack of one item on one tile.</summary>
    public sealed class GroundItem
    {
        internal GroundItem(int id, Item item, int count, int tileX, int tileY)
        {
            Id = id; Item = item; Count = count; TileX = tileX; TileY = tileY;
        }

        /// <summary>Unique within its <see cref="GroundItems"/>.</summary>
        public int Id { get; }
        public Item Item { get; }
        /// <summary>1 to <see cref="Item.MaxStack"/>. Show it when above 1.</summary>
        public int Count { get; internal set; }
        public int TileX { get; }
        public int TileY { get; }

        public override string ToString() => Count > 1 ? Item.Name + " x" + Count : Item.Name;
    }

    public enum PickUpOutcome
    {
        /// <summary>Nothing (pickable) within reach.</summary>
        NothingThere,
        /// <summary>All of it went into the inventory.</summary>
        PickedUp,
        /// <summary>Only part fitted (a stack with some room); the rest stays on the ground. <see cref="PickUpResult.Message"/> is the capacity text.</summary>
        Partial,
        /// <summary>No free slot and no stack room: nothing taken, the item stays. <see cref="PickUpResult.Message"/> is <see cref="GroundItems.CapacityText"/>.</summary>
        NoRoom,
        /// <summary>A fallen (dead) player cannot pick things up.</summary>
        Fallen,
    }

    public sealed class PickUpResult
    {
        internal PickUpResult(PickUpOutcome outcome, GroundItem item = null, int taken = 0, int remaining = 0, string message = null)
        {
            Outcome = outcome; Item = item; Taken = taken; Remaining = remaining; Message = message;
        }

        public PickUpOutcome Outcome { get; }
        /// <summary>The ground item that was tried (null for NothingThere / Fallen). After a full pickup it is no longer on the ground.</summary>
        public GroundItem Item { get; }
        /// <summary>How many went into the inventory.</summary>
        public int Taken { get; }
        /// <summary>How many are still on the ground.</summary>
        public int Remaining { get; }
        /// <summary>The text to pop up (exactly "Inventory currently at capacity." for NoRoom and Partial), else null.</summary>
        public string Message { get; }
        public bool Success => Outcome == PickUpOutcome.PickedUp || Outcome == PickUpOutcome.Partial;
    }

    /// <summary>
    /// The items lying on one map (#36, #62). Dyllan: "enemies slain leave the items where they are dropped, player choses to pick them up or not.
    /// If players inventory slots are all filled a message pops up. 'Inventory currently at capacity.' With this also add a drop functionality
    /// to inventory items, players can drop items to pick up new ones." Not saved; no despawn timer; they vanish when the player leaves the map.
    /// </summary>
    public sealed class GroundItems
    {
        /// <summary>The exact pop-up text when an item cannot be picked up for lack of room.</summary>
        public const string CapacityText = "Inventory currently at capacity.";
        /// <summary>PLACEHOLDER pickup reach: the player's own tile or any of the 8 neighbouring tiles.</summary>
        public const int PickUpReach = 1;

        private readonly List<GroundItem> _items = new List<GroundItem>();
        private int _nextId = 1;

        /// <summary>Raised whenever something is added, removed or changes count.</summary>
        public event Action Changed;
        /// <summary>Goes up on every change; the UI can redraw when it moves.</summary>
        public int Version { get; private set; }

        /// <summary>Everything on the ground, oldest first. Snapshot copy.</summary>
        public IReadOnlyList<GroundItem> Items => _items.ToArray();
        public int Count => _items.Count;

        private void Touch() { Version++; Changed?.Invoke(); }

        /// <summary>Everything on one tile.</summary>
        public IReadOnlyList<GroundItem> At(int tileX, int tileY)
        {
            var r = new List<GroundItem>();
            foreach (var g in _items) if (g.TileX == tileX && g.TileY == tileY) r.Add(g);
            return r;
        }

        /// <summary>
        /// Puts items on a tile. Same item already there with room is topped up first; the rest goes into new stacks
        /// (so a tile may hold several). Returns the last ground item touched.
        /// </summary>
        public GroundItem Add(Item item, int count, int tileX, int tileY)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            GroundItem last = null;
            foreach (var g in _items)
            {
                if (count == 0) break;
                if (g.TileX != tileX || g.TileY != tileY || g.Item.Id != item.Id || g.Count >= item.MaxStack) continue;
                int add = Math.Min(count, item.MaxStack - g.Count);
                g.Count += add; count -= add; last = g;
            }
            while (count > 0)
            {
                int put = Math.Min(count, item.MaxStack);
                last = new GroundItem(_nextId++, item, put, tileX, tileY);
                _items.Add(last);
                count -= put;
            }
            Touch();
            return last;
        }

        /// <summary>The closest item to the player: standing-on first, then straight neighbours, then diagonals (within <see cref="PickUpReach"/>). Null if none.</summary>
        public GroundItem FindNear(int playerX, int playerY)
        {
            GroundItem best = null; int bestRank = int.MaxValue;
            foreach (var g in _items)
            {
                int dx = Math.Abs(g.TileX - playerX), dy = Math.Abs(g.TileY - playerY);
                if (dx > PickUpReach || dy > PickUpReach) continue;
                int rank = dx == 0 && dy == 0 ? 0 : (dx == 0 || dy == 0 ? 1 : 2);
                if (rank < bestRank) { best = g; bestRank = rank; }
            }
            return best;
        }

        /// <summary>Picks up whatever is nearest to the player (see <see cref="FindNear"/>): the pickup key on a tile.</summary>
        public PickUpResult TryPickUp(Character character, int playerX, int playerY)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (character.IsDead) return new PickUpResult(PickUpOutcome.Fallen);
            var g = FindNear(playerX, playerY);
            return g == null ? new PickUpResult(PickUpOutcome.NothingThere) : TryPickUp(character, g);
        }

        /// <summary>Picks up one specific ground item (for example one the player clicked). Takes what fits and leaves the rest.</summary>
        public PickUpResult TryPickUp(Character character, GroundItem item)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (item == null || !_items.Contains(item)) return new PickUpResult(PickUpOutcome.NothingThere);
            if (character.IsDead) return new PickUpResult(PickUpOutcome.Fallen);

            int fits = Math.Min(item.Count, character.Inventory.Room(item.Item));
            if (fits < 1) return new PickUpResult(PickUpOutcome.NoRoom, item, 0, item.Count, CapacityText);
            int taken = character.Inventory.Add(item.Item, fits);
            item.Count -= taken;
            if (item.Count < 1) _items.Remove(item);
            Touch();
            return item.Count < 1
                ? new PickUpResult(PickUpOutcome.PickedUp, item, taken, 0)
                : new PickUpResult(PickUpOutcome.Partial, item, taken, item.Count, CapacityText);
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _items.Clear();
            Touch();
        }
    }

    /// <summary>One <see cref="GroundItems"/> per map id. <see cref="WarInLocked.Core.Characters.Character.EnterMap"/> empties them all when the player changes map, so only the current map ever holds items.</summary>
    public sealed class GroundItemsByMap
    {
        private readonly Dictionary<string, GroundItems> _maps = new Dictionary<string, GroundItems>();

        public GroundItems ForMap(string mapId)
        {
            string key = mapId ?? "";
            if (!_maps.TryGetValue(key, out var g)) { g = new GroundItems(); _maps[key] = g; }
            return g;
        }

        /// <summary>Forgets everything on every map (e.g. a new game).</summary>
        public void Clear() { foreach (var g in _maps.Values) g.Clear(); }
    }
}

namespace WarInLocked.Core.Items
{
    /// <summary>Result of <see cref="Character.DropItem"/>.</summary>
    public sealed class DropResult
    {
        internal DropResult(bool dropped, Item item, int count, string message, GroundItem ground = null)
        {
            Dropped = dropped; Item = item; Count = count; Message = message; Ground = ground;
        }
        public bool Dropped { get; }
        public Item Item { get; }
        /// <summary>How many were put on the ground.</summary>
        public int Count { get; }
        /// <summary>A refusal text to show (quest item, fallen), else null.</summary>
        public string Message { get; }
        /// <summary>The ground item they ended up in.</summary>
        public GroundItem Ground { get; }
    }
}
