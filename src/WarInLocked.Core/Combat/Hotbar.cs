using System;

namespace WarInLocked.Core.Combat
{
    /// <summary>One hotbar slot: an ability id (or empty) plus its cooldown.</summary>
    public sealed class HotbarSlot
    {
        /// <summary>The ability in this slot, e.g. "slash", or null if the slot is empty.</summary>
        public string AbilityId { get; internal set; }
        /// <summary>The consumable item in this slot (#26), or null. A slot holds an ability OR an item, never both.</summary>
        public string ItemId { get; internal set; }
        public bool IsItem => ItemId != null;
        public bool IsEmpty => AbilityId == null && ItemId == null;
        /// <summary>Seconds until the ability can be used again (0 = ready).</summary>
        public double CooldownRemaining { get; internal set; }
        /// <summary>The full length of the cooldown that is running.</summary>
        public double CooldownDuration { get; internal set; }
        public bool IsReady => CooldownRemaining <= 0;
        /// <summary>For the cooldown overlay: 1 right after use, falling to 0 when ready.</summary>
        public double CooldownProgress => CooldownRemaining <= 0 || CooldownDuration <= 0 ? 0 : CooldownRemaining / CooldownDuration;
    }

    /// <summary>
    /// The ability hotbar: 5 slots, index 0-4 for the keys 1-5 (#11). Learned abilities are placed automatically
    /// by <see cref="CombatController"/> (Slash goes to slot 1).
    /// </summary>
    public sealed class Hotbar
    {
        public const int SlotCount = 5;
        private readonly HotbarSlot[] _slots = new HotbarSlot[SlotCount];

        public Hotbar()
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = new HotbarSlot();
        }

        public int Count => SlotCount;
        public HotbarSlot this[int index] => Get(index);

        public HotbarSlot Get(int index)
        {
            if (index < 0 || index >= SlotCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _slots[index];
        }

        /// <summary>The slot holding an ability, or -1.</summary>
        public int IndexOf(string abilityId)
        {
            for (int i = 0; i < SlotCount; i++) if (_slots[i].AbilityId == abilityId) return i;
            return -1;
        }

        /// <summary>Puts an ability into a slot (null empties it) and clears that slot's cooldown.</summary>
        public void Assign(int index, string abilityId)
        {
            var slot = Get(index);
            slot.AbilityId = abilityId;
            slot.ItemId = null;
            slot.CooldownRemaining = 0;
            slot.CooldownDuration = 0;
        }

        /// <summary>Puts a consumable item (by item id) into a slot (null empties it) and clears the slot's cooldown (#26). The item uses the shared item cooldown.</summary>
        public void AssignItem(int index, string itemId)
        {
            var slot = Get(index);
            slot.AbilityId = null;
            slot.ItemId = itemId;
            slot.CooldownRemaining = 0;
            slot.CooldownDuration = 0;
        }

        /// <summary>The slot holding an item, or -1.</summary>
        public int IndexOfItem(string itemId)
        {
            for (int i = 0; i < SlotCount; i++) if (_slots[i].ItemId == itemId) return i;
            return -1;
        }

        /// <summary>Puts an ability into the first empty slot unless it is already on the bar. Returns the slot index or -1 if full.</summary>
        public int AutoPlace(string abilityId)
        {
            int existing = IndexOf(abilityId);
            if (existing >= 0) return existing;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].IsEmpty) { Assign(i, abilityId); return i; }
            }
            return -1;
        }

        internal void StartCooldown(int index, double seconds)
        {
            var slot = Get(index);
            slot.CooldownDuration = seconds;
            slot.CooldownRemaining = seconds;
        }

        internal void Update(double seconds)
        {
            foreach (var s in _slots)
                if (s.CooldownRemaining > 0) s.CooldownRemaining = Math.Max(0, s.CooldownRemaining - seconds);
        }
    }
}
