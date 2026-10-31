using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Items
{
    /// <summary>
    /// A fixed number of slots (10 by default), each empty (null) or holding an <see cref="ItemStack"/> (issue #9).
    /// UI code reads <see cref="Slots"/> / <see cref="Get"/> and listens to <see cref="Changed"/>
    /// (or compares <see cref="Version"/> between frames) to know when to redraw.
    /// </summary>
    public sealed class Inventory
    {
        public const int DefaultCapacity = 10;

        private readonly ItemStack[] _slots;

        public Inventory(int capacity = DefaultCapacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _slots = new ItemStack[capacity];
        }

        /// <summary>Raised after any change to the contents.</summary>
        public event EventHandler Changed;

        /// <summary>Goes up by one on every change, for polling UIs.</summary>
        public int Version { get; private set; }

        public int Capacity => _slots.Length;

        /// <summary>Every slot in order; null means the slot is empty. Always <see cref="Capacity"/> long. Snapshot copy.</summary>
        public IReadOnlyList<ItemStack> Slots => (ItemStack[])_slots.Clone();

        /// <summary>The stack in a slot, or null if empty.</summary>
        public ItemStack Get(int index)
        {
            CheckIndex(index);
            return _slots[index];
        }

        public int FreeSlots
        {
            get
            {
                int n = 0;
                foreach (var s in _slots) if (s == null) n++;
                return n;
            }
        }

        public bool IsFull => FreeSlots == 0;

        /// <summary>How many of the item (by id) could still be added.</summary>
        public int Room(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            int room = 0;
            foreach (var s in _slots)
            {
                if (s == null) room += item.MaxStack;
                else if (s.Item.Id == item.Id) room += item.MaxStack - s.Count;
            }
            return room;
        }

        public bool CanAdd(Item item, int count = 1) => count >= 1 && Room(item) >= count;

        /// <summary>
        /// Adds up to <paramref name="count"/> items, topping up existing stacks first and then using empty slots.
        /// Returns how many were ACTUALLY added; anything less than <paramref name="count"/> means the inventory filled up
        /// (the rest is not added, nothing is lost silently).
        /// </summary>
        public int Add(Item item, int count = 1)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

            int remaining = count;
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                var s = _slots[i];
                if (s == null || s.Item.Id != item.Id) continue;
                int add = Math.Min(remaining, item.MaxStack - s.Count);
                if (add <= 0) continue;
                _slots[i] = new ItemStack(s.Item, s.Count + add);
                remaining -= add;
            }
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i] != null) continue;
                int add = Math.Min(remaining, item.MaxStack);
                _slots[i] = new ItemStack(item, add);
                remaining -= add;
            }

            int added = count - remaining;
            if (added > 0) NotifyChanged();
            return added;
        }

        /// <summary>Removes up to <paramref name="count"/> from one slot. Returns how many were removed (0 if the slot is empty).</summary>
        public int Remove(int index, int count = 1)
        {
            CheckIndex(index);
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var s = _slots[index];
            if (s == null) return 0;
            int removed = Math.Min(count, s.Count);
            _slots[index] = removed == s.Count ? null : new ItemStack(s.Item, s.Count - removed);
            NotifyChanged();
            return removed;
        }

        /// <summary>Removes up to <paramref name="count"/> of an item (by id) from any slots, last slots first. Returns how many were removed.</summary>
        public int RemoveItem(string itemId, int count = 1)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            int remaining = count;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                var s = _slots[i];
                if (s == null || s.Item.Id != itemId) continue;
                int take = Math.Min(remaining, s.Count);
                _slots[i] = take == s.Count ? null : new ItemStack(s.Item, s.Count - take);
                remaining -= take;
            }
            int removed = count - remaining;
            if (removed > 0) NotifyChanged();
            return removed;
        }

        /// <summary>Total number of an item (by id) held.</summary>
        public int CountOf(string itemId)
        {
            int n = 0;
            foreach (var s in _slots) if (s != null && s.Item.Id == itemId) n += s.Count;
            return n;
        }

        public bool Contains(string itemId) => CountOf(itemId) > 0;

        /// <summary>Index of the first slot holding the item, or -1.</summary>
        public int IndexOf(string itemId)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null && _slots[i].Item.Id == itemId) return i;
            return -1;
        }

        /// <summary>Swaps two slots (empty slots allowed). For drag and drop style UIs.</summary>
        public void Swap(int a, int b)
        {
            CheckIndex(a);
            CheckIndex(b);
            if (a == b) return;
            var t = _slots[a];
            _slots[a] = _slots[b];
            _slots[b] = t;
            NotifyChanged();
        }

        /// <summary>Low level: puts a stack in a slot and returns what was there (or null). Used by <see cref="Equipment"/> to swap.</summary>
        public ItemStack Replace(int index, ItemStack stack)
        {
            CheckIndex(index);
            var old = _slots[index];
            _slots[index] = stack;
            NotifyChanged();
            return old;
        }

        private void CheckIndex(int index)
        {
            if (index < 0 || index >= _slots.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }

        private void NotifyChanged()
        {
            Version++;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
