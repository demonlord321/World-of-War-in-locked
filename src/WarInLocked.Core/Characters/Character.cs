using System;
using System.Collections.Generic;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;
using WarInLocked.Core.World;

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

        private readonly GroundItemsByMap _ground = new GroundItemsByMap();
        /// <summary>The map the player is on, as told by <see cref="EnterMap"/> (null before the first call).</summary>
        public string CurrentMapId { get; private set; }
        /// <summary>The ground items by map id (not saved). Only the current map's can hold anything: <see cref="EnterMap"/> wipes the rest. <see cref="CombatController.Ground"/> is the current map's.</summary>
        public GroundItemsByMap GroundByMap => _ground;
        /// <summary>
        /// The Game calls this whenever a map is loaded: kills and drops now land on that map's ground. Dyllan: "items vanish when map refreshes.
        /// Leaving then returning. They remain so long as you do." So entering a different map (or coming back to one you left) starts with an
        /// empty ground; calling it again for the map the player is already on (same id) keeps the items. The Game only loads a map on a respawn
        /// when the bind point is on another map, so respawning on the same map keeps them.
        /// </summary>
        public void EnterMap(string mapId)
        {
            if (mapId != CurrentMapId) _ground.Clear();
            CurrentMapId = mapId;
            Combat.Ground = _ground.ForMap(mapId);
        }

        /// <summary>The 10 inventory slots (#9).</summary>
        public Inventory Inventory { get; }
        /// <summary>The worn gear (#10); swaps with <see cref="Inventory"/>.</summary>
        public Equipment Equipment { get; }
        /// <summary>Quests offered, active and finished (#8).</summary>
        public QuestLog Quests { get; }

        /// <summary>The in-game clock (#56): starts 8:00 am day 1, runs 5x real time, ticked by <c>Combat.Update</c>. See <see cref="GameClock"/>.</summary>
        public GameClock Clock { get; } = new GameClock();

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
        /// Max health = <c>HealthBase</c> x the Constitution modifier (a flat 0.1 per point, no milestone bonus since #46; see <c>StatModifier</c>),
        /// rounded half up. Warrior (Con 13) = 230, Merchant (Con 9) = 90, Mystic (Con 5) = 50; Con 10 = 200, Con 25 = 450 (#15).
        /// </summary>
        public const int HealthBase = 100;
        /// <summary>
        /// Max energy = <c>EnergyBase</c> x the Wisdom modifier, rounded half up (#17, placeholder base).
        /// Warrior (Wis 7) = 35, Merchant (Wis 9) = 45, Mystic (Wis 13) = 115; Wis 10 = 100.
        /// </summary>
        public const int EnergyBase = 50;

        /// <summary>Max health including the Death debuff's cut (#35): base x Constitution modifier x <see cref="DeathDebuffMultiplier"/>, rounded half up once.</summary>
        public int MaxHealth => (int)Math.Round(HealthBase * (StatModifier.Tenths(EffectiveStats.Constitution) / 10m) * (decimal)DeathDebuffMultiplier, MidpointRounding.AwayFromZero);
        /// <summary>Max energy including the Death debuff's cut (#35).</summary>
        public int MaxEnergy => (int)Math.Round(EnergyBase * (StatModifier.Tenths(EffectiveStats.Wisdom) / 10m) * (decimal)DeathDebuffMultiplier, MidpointRounding.AwayFromZero);

        // ---- Death debuff (#35) ----

        /// <summary>0 = none, 1-3 = Death debuff level (see <see cref="DeathDebuff"/>). A respawn adds a level while it is active.</summary>
        public int DeathDebuffLevel { get; private set; }
        /// <summary>Seconds left on the debuff (0 when none). Counts down in <c>Combat.Update</c> (so it pauses when the Game stops updating, and while fallen), in or out of combat.</summary>
        public double DeathDebuffRemaining { get; private set; }
        public bool HasDeathDebuff => DeathDebuffLevel > 0;
        /// <summary>1.0 without the debuff, else 0.75 / 0.5 / 0.25: applied to max health, max energy and damage dealt.</summary>
        public double DeathDebuffMultiplier => DeathDebuff.MultiplierFor(DeathDebuffLevel);
        /// <summary>"Death debuff 2", or null without the debuff.</summary>
        public string DeathDebuffName => DeathDebuff.NameFor(DeathDebuffLevel);
        /// <summary>"Death debuff 2: -50% health, attack and energy (0:42)" (level 3 adds ", no XP from kills"), or null without the debuff. Changes every second: for the HUD tooltip.</summary>
        public string DeathDebuffText => DeathDebuff.TextFor(DeathDebuffLevel, DeathDebuffRemaining);
        /// <summary>True while kills give XP (false at Death debuff level 3). Quest XP is not affected.</summary>
        public bool EarnsKillXp => DeathDebuffLevel < DeathDebuff.NoKillXpLevel;
        /// <summary>Raised when the debuff level changes (applied, stacked, expired). Not raised for the ticking timer.</summary>
        public event Action DeathDebuffChanged;

        private void ApplyDeathDebuffStack()
        {
            DeathDebuffLevel = Math.Min(DeathDebuff.MaxLevel, DeathDebuffLevel + 1);
            DeathDebuffRemaining = DeathDebuff.SecondsFor(DeathDebuffLevel);   // at level 3 a new death restarts the 60 s (guess)
            Version++;
            DeathDebuffChanged?.Invoke();
        }

        /// <summary>Counts the debuff down; at 0 the whole stack goes. Current health/energy stay where they are (they were already capped at the lower maximum). <c>Combat.Update</c> calls this. Frozen while fallen.</summary>
        internal void TickDeathDebuff(double seconds)
        {
            if (DeathDebuffLevel == 0 || IsDead) return;
            DeathDebuffRemaining = Math.Max(0, DeathDebuffRemaining - seconds);
            if (DeathDebuffRemaining > 0) return;
            DeathDebuffLevel = 0;
            Version++;
            DeathDebuffChanged?.Invoke();
        }

        /// <summary>The modifier of a stat's effective value (with the equipped title's bonus; milestones count the effective value, #27) (a flat 0.1 per point), e.g. 1.3 for 13 Constitution.</summary>
        public double Modifier(StatType stat) => StatModifier.Value(EffectiveStats[stat]);

        /// <summary>Current health, 0..<see cref="MaxHealth"/>. Starts full.</summary>
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;

        private double _energy;
        /// <summary>Current energy (whole points), 0..<see cref="MaxEnergy"/>. Starts full. Regenerates in <c>Combat.Update</c>.</summary>
        public int Energy => (int)Math.Floor(_energy);
        /// <summary>Energy including the fraction, for a smooth bar.</summary>
        public double EnergyExact => _energy;

        /// <summary>PLACEHOLDER death handling: health is simply clamped at 0 (<see cref="IsDead"/>) and <see cref="Died"/> fires once; nothing else happens.</summary>
        /// <summary>Raised after the character loses health (not below 0), with the amount lost. Draw a hit flash / damage number.</summary>
        public event Action<int> Damaged;
        /// <summary>Raised once when health reaches 0.</summary>
        public event Action Died;

        /// <summary>Removes health (not below 0). Returns the amount actually lost.</summary>
        public int TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            bool wasDead = IsDead;
            int lost = Math.Min(amount, Health);
            Health -= lost;
            if (amount > 0) Combat.MarkInCombat();
            if (lost > 0) Damaged?.Invoke(lost);
            if (!wasDead && IsDead)
            {
                RespawnRemaining = RespawnDelaySeconds;
                Died?.Invoke();
            }
            return lost;
        }

        // ---- Respawn (#29) ----
        /// <summary>The headline shown when the character falls (Dyllan, #29, verbatim).</summary>
        public const string FallenTitle = "You have fallen...";
        /// <summary>The line under the headline (Dyllan, #29, verbatim).</summary>
        public const string FallenSubtitle = "The goddess has seen fit to grant you another chance.";
        /// <summary>The whole message as one string.</summary>
        public const string FallenMessage = FallenTitle + " " + FallenSubtitle;
        /// <summary>The character respawns automatically this long after falling (Dyllan: 5 s).</summary>
        public const double RespawnDelaySeconds = 5.0;

        /// <summary>Seconds until the respawn while fallen (counts down in <see cref="UpdateRespawn"/>, which <c>Combat.Update</c> calls); 0 when alive.</summary>
        public double RespawnRemaining { get; private set; }
        /// <summary>0 just after falling up to 1 at the respawn; 0 when alive. For a countdown bar.</summary>
        public double RespawnProgress => IsDead ? 1.0 - RespawnRemaining / RespawnDelaySeconds : 0.0;

        /// <summary>
        /// Where the character respawns. Starts as the tutorial bind point, next to Colonel Powers. Only one is kept: entering a new town
        /// does not move it, only <see cref="SetBindPoint"/> does.
        /// </summary>
        public BindPoint BindPoint { get; private set; } = BindPoint.Tutorial;

        /// <summary>Sets the bind point at an inn or rest location, replacing the old one.</summary>
        public void SetBindPoint(BindPoint point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            BindPoint = point;
            Version++;
        }

        /// <summary>
        /// Raised when the respawn happens, with the bind point to move the player to (map and tile). The character is alive again,
        /// at full health and energy. The screen should move the player, call <see cref="CombatController.CalmEnemies"/> and clear its death overlay.
        /// </summary>
        public event Action<BindPoint> Respawned;

        /// <summary>XP lost per respawn at levels 1-5 (Dyllan).</summary>
        public const int RespawnXpPenaltyLow = 50;
        /// <summary>XP lost per respawn at levels 6-10 (Dyllan).</summary>
        public const int RespawnXpPenaltyHigh = 100;
        /// <summary>XP lost per respawn at levels 11-20 (Dyllan, #30: "200 exp from 10 to 20"); it doubles every ten levels after that.</summary>
        public const int RespawnXpPenaltyLevel11 = 200;

        /// <summary>The most XP a respawn costs at a level (#30): 50 for levels 1-5, 100 for 6-10, 200 for 11-20, 400 for 21-30, 800 for 31-40, doubling every ten levels.
        /// Level 10 stays at 100 (Dyllan said 100 up to level 10); the 200 starts at level 11.</summary>
        public static int RespawnXpPenaltyForLevel(int level)
        {
            if (level <= 5) return RespawnXpPenaltyLow;
            if (level <= 10) return RespawnXpPenaltyHigh;
            int doublings = Math.Min((level - 11) / 10, 20);   // 20 doublings is far beyond any real level; it only guards against overflow
            return RespawnXpPenaltyLevel11 << doublings;
        }

        /// <summary>The most XP a respawn costs at the current level (see <see cref="RespawnXpPenaltyForLevel"/>). The actual loss can be less (see <see cref="LastRespawnXpLost"/>).</summary>
        public int RespawnXpPenalty => RespawnXpPenaltyForLevel(Level);
        /// <summary>The XP the last respawn actually took (set before <see cref="Respawned"/> fires; never below the current level's starting XP, never below 0). 0 before any respawn. Show "-50 XP".</summary>
        public int LastRespawnXpLost { get; private set; }

        /// <summary>Counts the respawn down while fallen and respawns at 0. Does nothing while alive. <c>Combat.Update</c> calls this, so the screen needs no extra call.</summary>
        public void UpdateRespawn(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsDead) return;
            RespawnRemaining = Math.Max(0, RespawnRemaining - seconds);
            if (RespawnRemaining > 0) return;
            // Dyllan: full health and energy, and the XP penalty. PLACEHOLDER: combat window cleared. Quests, copper and items are untouched.
            long floor = Leveling.TotalXpFor(Level);   // never de-level (placeholder)
            int lost = (int)Math.Max(0, Math.Min(RespawnXpPenalty, Xp - floor));
            Xp -= lost;
            LastRespawnXpLost = lost;
            ApplyDeathDebuffStack();   // #35: before the refill, so you come back at the reduced maximum
            Health = MaxHealth;
            _energy = MaxEnergy;
            Combat.ClearCombat();
            Version++;
            Respawned?.Invoke(BindPoint);
        }

        /// <summary>Health and energy back to the maximum at once (resting, #55). Does nothing while fallen.</summary>
        public void RecoverFully()
        {
            if (IsDead) return;
            Health = MaxHealth;
            _energy = MaxEnergy;
            Version++;
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
            if (IsDead) return ItemUseResult.FallenText;
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
        public const string CannotDropQuestItemText = "Quest items can't be dropped";
        public const string CannotDropFallenText = "Can't drop items while fallen";

        /// <summary>
        /// Drops items from an inventory slot onto the ground of the current map at the given tile (the player's tile), so the player can make room
        /// (#62). Worn equipment is not in the inventory, so it has to be unequipped first. Quest items cannot be dropped and a fallen player cannot
        /// drop (both GUESSES). Dropped items can be picked up again with <see cref="GroundItems.TryPickUp(Character,int,int)"/>.
        /// </summary>
        public DropResult DropItem(int slot, int count, int tileX, int tileY)
        {
            if (IsDead) return new DropResult(false, null, 0, CannotDropFallenText);
            var stack = slot >= 0 && slot < Inventory.Capacity ? Inventory.Get(slot) : null;
            if (stack == null || count < 1) return new DropResult(false, null, 0, null);
            if (stack.Item.IsQuestItem) return new DropResult(false, stack.Item, 0, CannotDropQuestItemText);
            var item = stack.Item;
            int n = Inventory.Remove(slot, Math.Min(count, stack.Count));
            var g = Combat.Ground.Add(item, n, tileX, tileY);
            return new DropResult(true, item, n, null, g);
        }

        /// <summary>Drops the whole stack in the slot.</summary>
        public DropResult DropStack(int slot, int tileX, int tileY)
        {
            var stack = slot >= 0 && slot < Inventory.Capacity ? Inventory.Get(slot) : null;
            return DropItem(slot, stack == null ? 0 : stack.Count, tileX, tileY);
        }

        /// <summary>Picks up the nearest ground item of the current map around the player's tile (the pickup key). See <see cref="GroundItems.TryPickUp(Character,int,int)"/>.</summary>
        public PickUpResult PickUp(int tileX, int tileY) => Combat.Ground.TryPickUp(this, tileX, tileY);

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
        /// <summary>Every flag set so far (for saving, #54). Snapshot copy.</summary>
        public IReadOnlyList<string> Flags => new List<string>(_flags);
        public void SetFlag(string flag) => _flags.Add(flag);

        /// <summary>PLACEHOLDER text shown when the player tries to leave the Forest of Beginnings too early (Dyllan gave none).</summary>
        public const string LeaveForestRefusal = "Colonel Powers isn't finished with you yet.";

        /// <summary>
        /// Dyllan's rule: the player cannot leave the starting forest until Colonel Powers has given the "Adventurers start" quest
        /// (accepted, handed in or completed all count; merely offered does not). The forest exit checks this and shows <see cref="LeaveForestRefusal"/>.
        /// </summary>
        public bool CanLeaveForest
        {
            get
            {
                var q = Quests.Get(WarInLocked.Core.Quests.QuestLibrary.AdventurersStartId);
                return q != null && q.State != WarInLocked.Core.Quests.QuestState.Offered;
            }
        }

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

        // ---- Loading a save (#54): used by SaveGame only ----
        internal void RestoreTitles(IEnumerable<string> titleIds, string equippedId)
        {
            _titles.Clear();
            EquippedTitle = null;
            foreach (var id in titleIds)
            {
                var t = TitleLibrary.Get(id);
                if (t != null && !HasTitle(id)) _titles.Add(t);
            }
            if (equippedId != null && HasTitle(equippedId)) EquippedTitle = TitleLibrary.Get(equippedId);
        }

        internal void RestoreFlags(IEnumerable<string> flags)
        {
            _flags.Clear();
            foreach (var f in flags) _flags.Add(f);
        }

        /// <summary>Sets level, XP, points, health, energy and copper straight from a save. Call after the title is restored (it moves the maximums). A fallen save comes back with 1 health.</summary>
        internal void RestoreProgress(int level, int xp, int freeStatPoints, IList<int> assigned, int health, double energy, int copper)
        {
            Level = Math.Max(1, level);
            Xp = Math.Max(0, xp);
            FreeStatPoints = Math.Max(0, freeStatPoints);
            for (int i = 0; i < _assigned.Length; i++) _assigned[i] = assigned != null && i < assigned.Count ? assigned[i] : 0;
            Health = Math.Max(1, Math.Min(health, MaxHealth));
            _energy = Math.Max(0, Math.Min(energy, MaxEnergy));
            Copper = Math.Max(0, copper);
            RespawnRemaining = 0;
            Version++;
        }

        /// <summary>Restores the Death debuff from a save (#35). Call before <see cref="RestoreProgress"/> so health is capped at the reduced maximum.</summary>
        internal void RestoreDeathDebuff(int level, double remaining)
        {
            DeathDebuffLevel = Math.Max(0, Math.Min(DeathDebuff.MaxLevel, level));
            DeathDebuffRemaining = DeathDebuffLevel == 0 ? 0 : Math.Max(0.001, Math.Min(DeathDebuff.StackedSeconds, remaining));
        }

        internal void RestoreAbility(string abilityId, int level, int uses, int usesIntoLevel)
        {
            LearnAbility(abilityId);
            GetAbility(abilityId)?.Restore(level, uses, usesIntoLevel);
        }

        public string Name { get; }
        public Race Race { get; }
        public StatDistribution Distribution { get; }
        /// <summary>The creation values plus any assigned points. This is what the I menu lets you assign; it does NOT include the equipped title's bonus (see <see cref="EffectiveStats"/>).</summary>
        public StatBlock Stats { get; private set; }

        /// <summary>
        /// <see cref="Stats"/> plus the equipped title's bonus (#27). Everything that uses a stat (modifiers, max health and energy, damage) reads this,
        /// so a title's bonus counts everywhere.
        /// </summary>
        public StatBlock EffectiveStats
        {
            get
            {
                if (EquippedTitle == null) return Stats;
                return new StatBlock(Stats.Strength + TitleBonus(StatType.Strength), Stats.Dexterity + TitleBonus(StatType.Dexterity),
                    Stats.Constitution + TitleBonus(StatType.Constitution), Stats.Wisdom + TitleBonus(StatType.Wisdom),
                    Stats.Intelligence + TitleBonus(StatType.Intelligence), Stats.Perception + TitleBonus(StatType.Perception),
                    Stats.Charisma + TitleBonus(StatType.Charisma), Stats.Luck + TitleBonus(StatType.Luck));
            }
        }

        /// <summary>What the equipped title adds to a stat (0 when no title is equipped): the title's flat bonus, e.g. +2 for the Adventurer (#33: no milestone cap any more).</summary>
        public int TitleBonus(StatType stat) => EquippedTitle == null ? 0 : TitleBonusFor(EquippedTitle, stat);

        /// <summary>What a title would add to a stat (worn or not): its flat <see cref="Title.AllStatsBonus"/>. For previews.</summary>
        public int TitleBonusFor(Title title, StatType stat) => title == null ? 0 : title.AllStatsBonus;

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
            int now = 0, after = 0;
            foreach (var stat in StatTypes.All) { now += TitleBonus(stat); after += TitleBonusFor(t, stat); }
            ChangeStatsKeepingVitals(after > now, () => EquippedTitle = t);
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
