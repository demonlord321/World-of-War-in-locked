using System;
using System.Collections.Generic;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Characters
{
    /// <summary>A created character. Build one with <see cref="CharacterCreation"/>.</summary>
    public sealed class Character
    {
        public Character(string name, Race race, StatDistribution distribution, StatBlock stats)
        {
            Name = name;
            Race = race;
            Distribution = distribution;
            Stats = stats;
            Inventory = new Inventory();
            Equipment = new Equipment(Inventory);
            Quests = new QuestLog(this);
            Combat = new CombatController(this);
            Health = MaxHealth;
            _energy = MaxEnergy;
        }

        /// <summary>The 10 inventory slots (#9).</summary>
        public Inventory Inventory { get; }
        /// <summary>The worn gear (#10); swaps with <see cref="Inventory"/>.</summary>
        public Equipment Equipment { get; }
        /// <summary>Quests offered, active and finished (#8).</summary>
        public QuestLog Quests { get; }

        /// <summary>Real-time combat rules, hotbar and cooldowns (#11). Call <c>Combat.Update(seconds)</c> every frame.</summary>
        public CombatController Combat { get; }

        /// <summary>Starts at 1 and rises as <see cref="Xp"/> grows (#13); see <see cref="Leveling"/>.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>XP gathered inside the current level (0 up to <see cref="XpForNextLevel"/> - 1).</summary>
        public int XpIntoLevel => (int)(Xp - Leveling.TotalXpFor(Level));
        /// <summary>XP that the current level needs in total before the next one.</summary>
        public int XpForNextLevel => Leveling.CostToNext(Level);
        /// <summary>0..1 for the XP bar.</summary>
        public float XpFraction => (float)XpIntoLevel / XpForNextLevel;

        /// <summary>Stat points earned and not yet spent.</summary>
        public int FreeStatPoints { get; private set; }
        private readonly int[] _assigned = new int[StatTypes.Count];
        /// <summary>How many points have been put into a stat so far (what <see cref="UnassignPoint"/> can take back).</summary>
        public int AssignedPoints(StatType stat) => _assigned[(int)stat];

        /// <summary>Raised once for every level gained, with the new level. Handy for a "Level up!" notice.</summary>
        public event Action<int> LeveledUp;
        /// <summary>Bumps on every level-up, stat point change and copper change; poll it to refresh a UI.</summary>
        public int Version { get; private set; }

        /// <summary>
        /// Puts one free point into a stat. Returns false if there are none left.
        /// Constitution and Wisdom raise the maximum health/energy, and the current value rises by the same amount.
        /// </summary>
        public bool AssignPoint(StatType stat)
        {
            if (FreeStatPoints <= 0) return false;
            ChangeStat(stat, +1);
            FreeStatPoints--;
            Version++;
            return true;
        }

        /// <summary>Takes a previously assigned point back (never below the stat's creation value). Returns false if none was assigned there.</summary>
        public bool UnassignPoint(StatType stat)
        {
            if (_assigned[(int)stat] <= 0) return false;
            ChangeStat(stat, -1);
            FreeStatPoints++;
            Version++;
            return true;
        }

        private void ChangeStat(StatType stat, int delta)
        {
            ChangeStatsKeepingVitals(delta > 0, () =>
            {
                Stats = Stats.WithAdded(stat, delta);
                _assigned[(int)stat] += delta;
            });
        }

        /// <summary>
        /// Runs a change that can move MaxHealth/MaxEnergy (a stat point, a title) and moves the current health and energy by the same amount:
        /// up when <paramref name="gain"/> is true; when it is false it never kills and never drops health below 1.
        /// </summary>
        private void ChangeStatsKeepingVitals(bool gain, Action change)
        {
            int oldHealth = MaxHealth, oldEnergy = MaxEnergy;
            change();
            int dh = MaxHealth - oldHealth, de = MaxEnergy - oldEnergy;
            if (gain)
            {
                if (dh != 0 && Health > 0) Health += dh;
                _energy += de;
            }
            else
            {
                // Giving a point back lowers the current value too, but never kills and never drops below 1 health.
                if (dh != 0 && Health > 0) Health = Math.Max(1, Health + dh);
                _energy = Math.Max(0, _energy + de);
            }
            Health = Math.Min(Health, MaxHealth);
            _energy = Math.Min(_energy, MaxEnergy);
        }

        /// <summary>
        /// Max health = <c>HealthBase</c> x the Constitution modifier (0.1 per point, +1.0 per milestone; see <c>StatModifier</c>),
        /// rounded half up. Warrior (Con 13) = 230, Merchant (Con 9) = 90, Mystic (Con 5) = 50; Con 10 = 200, Con 25 = 450 (#15).
        /// </summary>
        public const int HealthBase = 100;
        /// <summary>
        /// Max energy = <c>EnergyBase</c> x the Wisdom modifier, rounded half up (#17, placeholder base).
        /// Warrior (Wis 7) = 35, Merchant (Wis 9) = 45, Mystic (Wis 13) = 115; Wis 10 = 100.
        /// </summary>
        public const int EnergyBase = 50;

        public int MaxHealth => (int)Math.Round(HealthBase * (StatModifier.Tenths(EffectiveStats.Constitution) / 10m), MidpointRounding.AwayFromZero);
        public int MaxEnergy => (int)Math.Round(EnergyBase * (StatModifier.Tenths(EffectiveStats.Wisdom) / 10m), MidpointRounding.AwayFromZero);

        /// <summary>The modifier of a stat's effective value (with the equipped title's bonus; milestones count the effective value, #27) (0.1 per point, +1.0 per milestone), e.g. 2.3 for 13 Constitution.</summary>
        public double Modifier(StatType stat) => StatModifier.Value(EffectiveStats[stat]);

        /// <summary>Current health, 0..<see cref="MaxHealth"/>. Starts full.</summary>
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;

        private double _energy;
        /// <summary>Current energy (whole points), 0..<see cref="MaxEnergy"/>. Starts full. Regenerates in <c>Combat.Update</c>.</summary>
        public int Energy => (int)Math.Floor(_energy);
        /// <summary>Energy including the fraction, for a smooth bar.</summary>
        public double EnergyExact => _energy;

        /// <summary>Removes health (not below 0). Returns the amount actually lost.</summary>
        public int TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int lost = Math.Min(amount, Health);
            Health -= lost;
            if (amount > 0) Combat.MarkInCombat();
            return lost;
        }

        public void Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Min(MaxHealth, Health + amount);
        }

        /// <summary>Spends energy if there is enough. Returns false (and spends nothing) otherwise.</summary>
        public bool SpendEnergy(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (_energy < amount) return false;
            _energy -= amount;
            return true;
        }

        public void RegenEnergy(double amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            _energy = Math.Min(MaxEnergy, _energy + amount);
        }

        public int Xp { get; private set; }

        /// <summary>Copper coins (#21), the lowest currency. Never negative. Starts at 0.</summary>
        public int Copper { get; private set; }
        /// <summary>Raised when <see cref="Copper"/> changes, with the new total. <see cref="Version"/> also bumps.</summary>
        public event Action<int> CopperChanged;

        /// <summary>Adds copper (0 or more). Returns the new total.</summary>
        public int GainCopper(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return Copper;
            Copper += amount;
            Version++;
            CopperChanged?.Invoke(Copper);
            return Copper;
        }

        public bool CanAfford(int amount) => amount >= 0 && Copper >= amount;

        /// <summary>Spends copper if there is enough. Returns false (and spends nothing) otherwise.</summary>
        public bool SpendCopper(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Copper < amount) return false;
            if (amount == 0) return true;
            Copper -= amount;
            Version++;
            CopperChanged?.Invoke(Copper);
            return true;
        }

        // ---- consumables (#26)

        /// <summary>True while in combat: dealt or took damage within the last <see cref="CombatController.CombatDurationSeconds"/> seconds (counts down in <c>Combat.Update</c>).</summary>
        public bool InCombat => Combat.InCombat;
        /// <summary>Seconds until the character counts as out of combat again (0 = out of combat).</summary>
        public double CombatCooldownRemaining => Combat.CombatCooldownRemaining;

        /// <summary>Seconds until another consumable can be used (0 = ready). Shared by all consumables; counts down in <c>Combat.Update</c>.</summary>
        public double ItemCooldownRemaining { get; private set; }
        /// <summary>The full length of the running item cooldown (for a cooldown overlay).</summary>
        public double ItemCooldownDuration { get; private set; }
        public bool ItemReady => ItemCooldownRemaining <= 0;

        internal void TickItemCooldown(double seconds)
        {
            if (ItemCooldownRemaining > 0) ItemCooldownRemaining = Math.Max(0, ItemCooldownRemaining - seconds);
        }

        /// <summary>
        /// Why this item cannot be used right now ("None left", "Cannot be used", "On cooldown", "Already at full health"), or null if it can.
        /// Using a healing item at full health is refused and nothing is spent.
        /// </summary>
        public string CanUse(string itemId)
        {
            var item = Inventory.Contains(itemId) ? ItemLibrary.Get(itemId) : null;
            if (item == null) return Inventory.Contains(itemId) ? ItemUseResult.CannotUseText : ItemUseResult.NoneLeftText;
            if (!item.IsUsable) return ItemUseResult.CannotUseText;
            if (item.RecoversVitals && InCombat) return ItemUseResult.InCombatText;
            if (!ItemReady) return ItemUseResult.OnCooldownText;
            if (item.RecoversVitals && Health >= MaxHealth && _energy >= MaxEnergy) return ItemUseResult.FullyRecoveredText;
            if (item.HealAmount > 0 && Health >= MaxHealth) return ItemUseResult.FullHealthText;
            return null;
        }

        /// <summary>
        /// Uses one consumable from the inventory: heals (capped at max health), removes one from the stack and starts the shared item
        /// cooldown (also shown on hotbar slots holding items). If refused (see <see cref="CanUse"/>) nothing changes.
        /// </summary>
        public ItemUseResult UseItem(string itemId)
        {
            string reason = CanUse(itemId);
            var item = ItemLibrary.Get(itemId);
            if (reason != null) return ItemUseResult.Refused(item, reason);

            int before = Health;
            double energyBefore = _energy;
            if (item.RecoversVitals)
            {
                // Up to level 10 everything comes back; above that half of the max (capped at max).
                bool full = Level <= Item.FullRecoveryMaxLevel;
                Heal(full ? MaxHealth : (int)Math.Round(MaxHealth * Item.ReducedRecoveryFraction, MidpointRounding.AwayFromZero));
                RegenEnergy(full ? MaxEnergy : MaxEnergy * Item.ReducedRecoveryFraction);
            }
            Heal(item.HealAmount);
            Inventory.RemoveItem(itemId, 1);
            ItemCooldownDuration = item.UseCooldownSeconds;
            ItemCooldownRemaining = item.UseCooldownSeconds;
            Combat.OnItemUsed(item.UseCooldownSeconds);
            Version++;
            return ItemUseResult.Used(item, Health - before, (int)Math.Floor(_energy) - (int)Math.Floor(energyBefore));
        }

        private readonly List<string> _abilities = new List<string>();
        /// <summary>Ability ids learned so far, e.g. "slash".</summary>
        public IReadOnlyList<string> Abilities => _abilities;

        private readonly HashSet<string> _flags = new HashSet<string>();
        public bool HasFlag(string flag) => _flags.Contains(flag);
        public void SetFlag(string flag) => _flags.Add(flag);

        /// <summary>Adds XP (total). Can level up several times at once. Returns how many levels were gained; each one grants free stat points.</summary>
        public int GainXp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Xp += amount;
            int gained = 0;
            while (Xp >= Leveling.TotalXpFor(Level + 1))
            {
                Level++;
                gained++;
                FreeStatPoints += Leveling.StatPointsForReaching(Level);
                Version++;
                LeveledUp?.Invoke(Level);
            }
            return gained;
        }

        private readonly List<Ability> _abilityStates = new List<Ability>();
        /// <summary>The learned abilities with their level and uses (#20), in the order they were learned. Ids unknown to <see cref="AbilityLibrary"/> have no entry.</summary>
        public IReadOnlyList<Ability> LearnedAbilities => _abilityStates;
        /// <summary>The level/uses state of a learned ability, or null if it is not learned.</summary>
        public Ability GetAbility(string abilityId)
        {
            foreach (var a in _abilityStates) if (a.Id == abilityId) return a;
            return null;
        }
        /// <summary>Raised when any learned ability levels up.</summary>
        public event Action<Ability> AbilityLeveledUp;

        /// <summary>Learns an ability (level 1). Returns false if it was already known.</summary>
        public bool LearnAbility(string abilityId)
        {
            if (_abilities.Contains(abilityId)) return false;
            _abilities.Add(abilityId);
            var def = AbilityLibrary.Get(abilityId);
            if (def != null)
            {
                var state = new Ability(def);
                state.LeveledUp += a => AbilityLeveledUp?.Invoke(a);
                _abilityStates.Add(state);
            }
            return true;
        }

        public string Name { get; }
        public Race Race { get; }
        public StatDistribution Distribution { get; }
        /// <summary>The creation values plus any assigned points. This is what the I menu lets you assign; it does NOT include the equipped title's bonus (see <see cref="EffectiveStats"/>).</summary>
        public StatBlock Stats { get; private set; }

        /// <summary>
        /// <see cref="Stats"/> plus the equipped title's bonus (#27). Everything that uses a stat (modifiers, max health and energy, damage) reads this,
        /// so a milestone (10, 25, 50) is reached by the effective value.
        /// </summary>
        public StatBlock EffectiveStats => TitleBonus(StatType.Strength) == 0 ? Stats : Stats.Plus(StatBlock.Uniform(EquippedTitle.AllStatsBonus));

        /// <summary>What the equipped title adds to a stat (0 when no title is equipped).</summary>
        public int TitleBonus(StatType stat) => EquippedTitle == null ? 0 : EquippedTitle.AllStatsBonus;

        private readonly List<Title> _titles = new List<Title>();
        /// <summary>Titles earned so far, in the order they were earned.</summary>
        public IReadOnlyList<Title> Titles => _titles.ToArray();
        /// <summary>The title being worn, or null. Only one at a time.</summary>
        public Title EquippedTitle { get; private set; }
        public bool HasTitle(string titleId) => _titles.Exists(t => t.Id == titleId);

        /// <summary>Raised when a title is earned, equipped or unequipped. <see cref="Version"/> also bumps.</summary>
        public event Action TitlesChanged;

        /// <summary>Earns a title (not equipped automatically). Returns false if the id is unknown or it was already earned.</summary>
        public bool GrantTitle(string titleId)
        {
            var t = TitleLibrary.Get(titleId);
            if (t == null || HasTitle(titleId)) return false;
            _titles.Add(t);
            Version++;
            TitlesChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Wears an earned title, replacing the one worn now. Max health/energy follow the bonus like assigning points does.
        /// Returns false if the title was not earned (or is already worn).
        /// </summary>
        public bool EquipTitle(string titleId)
        {
            if (!HasTitle(titleId) || (EquippedTitle != null && EquippedTitle.Id == titleId)) return false;
            var t = TitleLibrary.Get(titleId);
            bool gain = t.AllStatsBonus > (EquippedTitle?.AllStatsBonus ?? 0);
            ChangeStatsKeepingVitals(gain, () => EquippedTitle = t);
            Version++;
            TitlesChanged?.Invoke();
            return true;
        }

        /// <summary>Takes the worn title off. Current health/energy drop with the maximums (never below 1 health). Returns false if none was worn.</summary>
        public bool UnequipTitle()
        {
            if (EquippedTitle == null) return false;
            ChangeStatsKeepingVitals(false, () => EquippedTitle = null);
            Version++;
            TitlesChanged?.Invoke();
            return true;
        }
    }
}
