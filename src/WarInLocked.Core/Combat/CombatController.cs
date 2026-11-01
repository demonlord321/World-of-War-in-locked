using System;
using System.Collections.Generic;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Combat
{
    public enum CombatOutcome
    {
        /// <summary>No living enemy on a neighbouring tile (8-way). Nothing is spent.</summary>
        NoTarget,
        /// <summary>The attack cooldown or the ability's own cooldown is still running.</summary>
        OnCooldown,
        /// <summary>The ability needs a weapon in the main hand and there is none. Nothing is spent, no cooldown starts, no use is counted.</summary>
        NeedsWeapon,
        /// <summary>The ability costs more energy than the character has.</summary>
        NotEnoughEnergy,
        /// <summary>The hotbar slot is empty.</summary>
        EmptySlot,
        /// <summary>A consumable in a hotbar slot was used (#26); see <see cref="CombatResult.ItemResult"/>.</summary>
        ItemUsed,
        /// <summary>A consumable in a hotbar slot could not be used; <see cref="CombatResult.ItemResult"/> has the reason. Nothing is spent.</summary>
        ItemRefused,
        /// <summary>The target was hit and survived.</summary>
        Hit,
        /// <summary>The target was hit and died; quests were told (see <see cref="CombatResult.ReadyQuests"/>).</summary>
        Killed,
    }

    public sealed class CombatResult
    {
        private static readonly Quest[] None = new Quest[0];

        public CombatResult(CombatOutcome outcome, Enemy target = null, int damage = 0, IReadOnlyList<Quest> readyQuests = null,
            Ability abilityUsed = null, bool abilityLeveledUp = false, ItemUseResult itemResult = null)
        {
            ItemResult = itemResult;
            Outcome = outcome;
            Target = target;
            Damage = damage;
            ReadyQuests = readyQuests ?? None;
            AbilityUsed = abilityUsed;
            AbilityLeveledUp = abilityLeveledUp;
        }

        public CombatOutcome Outcome { get; }
        /// <summary>For <see cref="CombatOutcome.ItemUsed"/> / <see cref="CombatOutcome.ItemRefused"/>: what happened to the consumable.</summary>
        public ItemUseResult ItemResult { get; }
        public Enemy Target { get; }
        public int Damage { get; }
        /// <summary>Quests whose objectives were finished by this attack. They are now ReadyToTurnIn (no reward until turned in).</summary>
        public IReadOnlyList<Quest> ReadyQuests { get; }
        /// <summary>The ability that was cast (null for a basic attack or a failed attempt). Its Uses/Level already include this cast.</summary>
        public Ability AbilityUsed { get; }
        /// <summary>True if this cast raised <see cref="AbilityUsed"/> to a new level.</summary>
        public bool AbilityLeveledUp { get; }
        public bool Succeeded => Outcome == CombatOutcome.Hit || Outcome == CombatOutcome.Killed;
    }

    /// <summary>
    /// Real-time hotkey combat rules for one character (#11). Reachable as <c>character.Combat</c>.
    /// Call <see cref="Update"/> every frame; call <see cref="BasicAttack"/> for Space and <see cref="UseHotbar"/> for keys 1-5.
    /// Slimes do not fight back yet (see QUESTIONS.md).
    /// </summary>
    public sealed class CombatController
    {
        /// <summary>Seconds between attacks (shared by basic attack and abilities).</summary>
        public const double AttackCooldownSeconds = 0.5;
        /// <summary>PLACEHOLDER energy regeneration, energy per second.</summary>
        public const double EnergyRegenPerSecond = 2.0;
        /// <summary>Everyone's unarmed base attack (#14).</summary>
        public const int UnarmedBaseDamage = DamageCalculator.BaseAttack;

        private readonly Character _owner;

        public CombatController(Character owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Hotbar = new Hotbar();
        }

        public Hotbar Hotbar { get; }
        /// <summary>Seconds until the next attack is allowed (0 = ready).</summary>
        public double AttackCooldownRemaining { get; private set; }
        public bool AttackReady => AttackCooldownRemaining <= 0;

        /// <summary>Basic attack damage: (10 + main-hand Physical Attack) x Strength modifier, rounded half up; unarmed is 10 x the modifier.
        /// Pass a target to include its physical resistance multiplier (#24). See <see cref="DamageCalculator"/>.</summary>
        public static int BasicAttackDamage(Character character, Enemy target = null) =>
            DamageCalculator.PhysicalDamage(character, target?.PhysicalMultiplier ?? 1.0);

        /// <summary>
        /// Damage of an ability at a level, n = 5 + (level - 1), rounded half up once.
        /// Physical (Slash): (10 + main-hand Physical Attack + n) x Strength modifier. Magic (Energy Ball): (10 + main-hand Magical Attack + n) x Intelligence modifier (#22, #23).
        /// Pass a target to include its resistance multiplier for that damage type (#24); rounded half up once at the end.
        /// </summary>
        public static int AbilityDamage(Character character, AbilityDefinition ability, int level = 1, Enemy target = null)
        {
            int n = ability.PowerAt(level);
            double m = target?.MultiplierFor(ability.Kind) ?? 1.0;
            return ability.Kind == AbilityKind.Magic
                ? DamageCalculator.MagicDamage(character, DamageCalculator.MagicalAttackBonus(character) + n, m)
                : DamageCalculator.PhysicalDamage(character, DamageCalculator.PhysicalAttackBonus(character) + n, m);
        }

        /// <summary>Why the character cannot cast the ability right now for a reason the UI can show ("Needs a weapon"), or null if nothing blocks it.
        /// Cooldown, energy and target are checked at cast time and are not part of this.</summary>
        public static string CannotCastReason(Character character, AbilityDefinition ability) =>
            ability.RequiresWeapon && !DamageCalculator.HasWeapon(character) ? AbilityDefinition.NeedsWeaponText : null;

        /// <summary>Damage of a learned ability at its current level.</summary>
        public static int AbilityDamage(Character character, Ability ability, Enemy target = null) => AbilityDamage(character, ability.Definition, ability.Level, target);

        /// <summary>Advances cooldowns, regenerates energy and puts newly learned abilities on the hotbar.</summary>
        public void Update(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (AttackCooldownRemaining > 0) AttackCooldownRemaining = Math.Max(0, AttackCooldownRemaining - seconds);
            if (CombatCooldownRemaining > 0) CombatCooldownRemaining = Math.Max(0, CombatCooldownRemaining - seconds);
            _owner.UpdateRespawn(seconds);   // #29: counts down while fallen, then respawns
            Hotbar.Update(seconds);
            _owner.TickItemCooldown(seconds);
            _owner.RegenEnergy(EnergyRegenPerSecond * seconds);
            SyncHotbar();
        }

        /// <summary>You are in combat for this many seconds after dealing or taking damage (Dyllan: 15 s).</summary>
        public const double CombatDurationSeconds = 15.0;
        /// <summary>Seconds left of the in-combat window; 0 when out of combat.</summary>
        public double CombatCooldownRemaining { get; private set; }
        public bool InCombat => CombatCooldownRemaining > 0;
        /// <summary>Starts (or restarts) the in-combat window. Called when the player deals or takes damage.</summary>
        /// <summary>Ends the in-combat window at once (a respawn does this).</summary>
        public void ClearCombat() => CombatCooldownRemaining = 0;

        /// <summary>Makes every enemy passive again (aggro and attack timers reset). Call it from the <c>Character.Respawned</c> handler (#29).</summary>
        public static void CalmEnemies(IEnumerable<Enemy> enemies)
        {
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            foreach (var e in enemies) e.Calm();
        }

        public void MarkInCombat() => CombatCooldownRemaining = CombatDurationSeconds;

        /// <summary>Puts a consumable (by item id) on a hotbar slot, replacing what was there (#26). Keys 1-5 then use it.</summary>
        public void AssignItemToHotbar(int slotIndex, string itemId) => Hotbar.AssignItem(slotIndex, itemId);

        /// <summary>Called by <c>Character.UseItem</c>: every item slot on the bar shows the shared cooldown.</summary>
        internal void OnItemUsed(double cooldownSeconds)
        {
            for (int i = 0; i < Hotbar.SlotCount; i++)
                if (Hotbar[i].IsItem) Hotbar.StartCooldown(i, cooldownSeconds);
        }

        /// <summary>Puts every learned ability on the hotbar if it is not there yet (Slash lands in slot 1).</summary>
        public void SyncHotbar()
        {
            foreach (string id in _owner.Abilities)
                if (AbilityLibrary.Get(id) != null) Hotbar.AutoPlace(id);
        }

        /// <summary>Space: hits the nearest adjacent enemy with the basic attack.</summary>
        public CombatResult BasicAttack(int playerX, int playerY, IReadOnlyList<Enemy> enemies)
        {
            if (!AttackReady) return new CombatResult(CombatOutcome.OnCooldown);
            var target = FindTarget(playerX, playerY, enemies);
            if (target == null) return new CombatResult(CombatOutcome.NoTarget);

            AttackCooldownRemaining = AttackCooldownSeconds;
            return Strike(target, BasicAttackDamage(_owner, target));
        }

        /// <summary>Keys 1-5 (index 0-4): uses the ability in that hotbar slot.</summary>
        public CombatResult UseHotbar(int slotIndex, int playerX, int playerY, IReadOnlyList<Enemy> enemies)
        {
            SyncHotbar();
            var slot = Hotbar.Get(slotIndex);
            if (slot.IsItem)
            {
                var used = _owner.UseItem(slot.ItemId);
                return new CombatResult(used.Success ? CombatOutcome.ItemUsed : CombatOutcome.ItemRefused, itemResult: used);
            }
            var state = slot.IsEmpty ? null : _owner.GetAbility(slot.AbilityId);
            var ability = state?.Definition;
            if (ability == null) return new CombatResult(CombatOutcome.EmptySlot);
            if (CannotCastReason(_owner, ability) != null) return new CombatResult(CombatOutcome.NeedsWeapon);
            if (!AttackReady || !slot.IsReady) return new CombatResult(CombatOutcome.OnCooldown);
            if (_owner.Energy < ability.EnergyCost) return new CombatResult(CombatOutcome.NotEnoughEnergy);
            var target = FindTarget(playerX, playerY, enemies);
            if (target == null) return new CombatResult(CombatOutcome.NoTarget); // nothing is spent without a target

            _owner.SpendEnergy(ability.EnergyCost);
            Hotbar.StartCooldown(slotIndex, ability.CooldownSeconds);
            AttackCooldownRemaining = AttackCooldownSeconds;
            var result = Strike(target, AbilityDamage(_owner, state, target));

            // The cast fired: it is a use (levels the ability) and counts for "use X N times" quests.
            bool leveled = state.RecordUse();
            var ready = new List<Quest>(result.ReadyQuests);
            foreach (var q in _owner.Quests.RecordAbilityUse(ability.Id))
                if (!ready.Contains(q)) ready.Add(q);
            return new CombatResult(result.Outcome, result.Target, result.Damage, ready, state, leveled);
        }

        /// <summary>
        /// Drives aggro'd enemies (call once per frame with the same seconds as <see cref="Update"/>). Each aggro'd, living enemy
        /// on a tile next to the player (or on it) hits the player for its <see cref="Enemy.Attack"/> every
        /// <see cref="Enemy.AttackIntervalSeconds"/>; the damage counts as damage received (starts the combat window).
        /// Passive enemies never attack. A dead player is not attacked. Returns the damage the player lost this call.
        /// </summary>
        public int UpdateEnemies(IReadOnlyList<Enemy> enemies, double seconds, int playerX, int playerY)
        {
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            int total = 0;
            foreach (var e in enemies)
            {
                bool inReach = Math.Abs(playerX - e.TileX) <= 1 && Math.Abs(playerY - e.TileY) <= 1;
                if (e.TickAttack(seconds, inReach && !_owner.IsDead)) total += _owner.TakeDamage(e.Attack);
            }
            return total;
        }

        private CombatResult Strike(Enemy target, int damage)
        {
            int dealt = target.TakeDamage(damage);
            MarkInCombat();
            if (!target.IsDead) return new CombatResult(CombatOutcome.Hit, target, dealt);
            var completed = _owner.Quests.RecordKill(target.Type);
            return new CombatResult(CombatOutcome.Killed, target, dealt, completed);
        }

        /// <summary>The nearest living enemy on one of the 8 neighbouring tiles or the player's own tile; straight neighbours win over diagonals.</summary>
        public static Enemy FindTarget(int playerX, int playerY, IReadOnlyList<Enemy> enemies)
        {
            Enemy best = null;
            bool bestStraight = false;
            foreach (var e in enemies)
            {
                if (e.IsDead || Math.Abs(playerX - e.TileX) > 1 || Math.Abs(playerY - e.TileY) > 1) continue;
                bool straight = e.TileX == playerX || e.TileY == playerY;
                if (best == null || (straight && !bestStraight)) { best = e; bestStraight = straight; }
            }
            return best;
        }
    }
}
