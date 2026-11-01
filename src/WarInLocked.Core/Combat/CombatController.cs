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
            Ability abilityUsed = null, bool abilityLeveledUp = false, ItemUseResult itemResult = null,
            int xpGained = 0, int levelsGained = 0, Item dropped = null, GroundItem groundItem = null, bool xpBlocked = false)
        {
            XpBlocked = xpBlocked;
            XpGained = xpGained;
            LevelsGained = levelsGained;
            DroppedItem = dropped;
            GroundItem = groundItem;
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
        /// <summary>XP the kill gave (#36), already added to the character; 0 for anything but <see cref="CombatOutcome.Killed"/>. Show "+5 XP".</summary>
        public int XpGained { get; }
        /// <summary>True when the kill would have given XP but Death debuff level 3 is active (#35): <see cref="XpGained"/> is 0. Show "No XP while Death debuff 3".</summary>
        public bool XpBlocked { get; }
        /// <summary>Levels the kill's XP brought (usually 0); the character's <c>LeveledUp</c> event also fires.</summary>
        public int LevelsGained { get; }
        /// <summary>The item the kill dropped (#36), or null. It lies on the ground at the slime's tile (<see cref="GroundItem"/>); the player picks it up.</summary>
        public Item DroppedItem { get; }
        /// <summary>The ground item the kill created (null if nothing dropped). It lives in <see cref="CombatController.Ground"/> (the current map's).</summary>
        public GroundItem GroundItem { get; }
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
            if (CombatCooldownRemaining > 0)
            {
                CombatCooldownRemaining = Math.Max(0, CombatCooldownRemaining - seconds);
                if (CombatCooldownRemaining <= 0) _combatTimerRanOut = true;   // #31: angry slimes lose their aggro (see UpdateEnemies)
            }
            _owner.Clock.Update(seconds);    // #56: the game clock only runs while the Game keeps calling Update (it pauses in menus)
            _owner.UpdateRespawn(seconds);   // #29: counts down while fallen, then respawns
            _owner.TickDeathDebuff(seconds); // #35: the Death debuff runs out (frozen while fallen)
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
        private bool _combatTimerRanOut;
        /// <summary>Starts (or restarts) the in-combat window. Called when the player deals or takes damage.</summary>
        /// <summary>Ends the in-combat window at once (a respawn does this).</summary>
        public void ClearCombat() { CombatCooldownRemaining = 0; _combatTimerRanOut = false; }

        /// <summary>Makes every enemy passive again (aggro and attack timers reset). Call it from the <c>Character.Respawned</c> handler (#29).</summary>
        public static void CalmEnemies(IEnumerable<Enemy> enemies)
        {
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            foreach (var e in enemies) e.Calm();
        }

        public void MarkInCombat() { CombatCooldownRemaining = CombatDurationSeconds; _combatTimerRanOut = false; }

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
        /// <see cref="Enemy.AttackIntervalSeconds"/> (a cooldown: the first hit is immediate once the player is in reach, and reach is checked before every hit); the damage counts as damage received (starts the combat window).
        /// Passive enemies never attack. A dead player is not attacked. Returns the damage the player lost this call.
        /// </summary>
        public int UpdateEnemies(IReadOnlyList<Enemy> enemies, double seconds, int playerX, int playerY) =>
            UpdateEnemies(enemies, seconds, playerX, playerY, null);

        /// <summary>
        /// Same as the other overload, and aggro'd enemies that are out of reach also chase (#31): one tile toward the player every
        /// <see cref="Enemy.ChaseStepSeconds"/> (8 directions), never onto the player or another living enemy (<see cref="TileOccupancy"/>), then attack
        /// in the same call if the step brought them into reach and their cooldown is ready. Read the new <see cref="Enemy.TileX"/> / <see cref="Enemy.TileY"/> afterwards to draw them.
        /// <paramref name="isWalkable"/> (optional) says whether an enemy may stand on a tile: the screen supplies walls, NPCs, doors and map bounds. A diagonal step
        /// also needs both tiles it passes beside to be walkable (no cutting wall corners). Null means every tile is walkable. A dead player is not chased.
        /// Chasing ends with the combat timer (#31, Dyllan: "If you run out the combat timer, then the slimes lose their aggro"): when the 15 s out-of-combat
        /// window has run out (no damage dealt or taken, see <see cref="CombatDurationSeconds"/>), the next call calms every aggro'd enemy in the list
        /// (<see cref="Enemy.Calm"/>: passive again, healed to full, no chase). They stay where they are and the screen lets them wander again. There is no distance-based leash.
        /// </summary>
        public int UpdateEnemies(IReadOnlyList<Enemy> enemies, double seconds, int playerX, int playerY, Func<int, int, bool> isWalkable)
        {
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            int total = 0;
            if (_combatTimerRanOut)
            {
                _combatTimerRanOut = false;
                foreach (var e in enemies) if (e != null && e.IsAggro && !e.IsDead) e.Calm();
            }
            foreach (var e in enemies)
            {
                if (e.IsAggro && !e.IsDead) Chase(e, seconds, playerX, playerY, enemies, isWalkable);
                bool inReach = Math.Abs(playerX - e.TileX) <= 1 && Math.Abs(playerY - e.TileY) <= 1;
                if (e.TickAttack(seconds, inReach && !_owner.IsDead)) total += _owner.TakeDamage(e.Attack);
            }
            return total;
        }

        private void Chase(Enemy e, double seconds, int playerX, int playerY, IReadOnlyList<Enemy> enemies, Func<int, int, bool> isWalkable)
        {
            if (_owner.IsDead || (Math.Abs(playerX - e.TileX) <= 1 && Math.Abs(playerY - e.TileY) <= 1))
            {
                e.ChaseTimer = 0;   // in reach (or nothing to chase): it stands and fights
                return;
            }
            e.ChaseTimer += seconds;
            while (e.ChaseTimer >= Enemy.ChaseStepSeconds)
            {
                if (!TryChaseStep(e, playerX, playerY, enemies, isWalkable))
                {
                    e.ChaseTimer = Enemy.ChaseStepSeconds;   // blocked: stay ready and try again next call, do not store up a burst of steps
                    return;
                }
                e.ChaseTimer -= Enemy.ChaseStepSeconds;
                if (Math.Abs(playerX - e.TileX) <= 1 && Math.Abs(playerY - e.TileY) <= 1) { e.ChaseTimer = 0; return; }
            }
        }

        private static bool TryChaseStep(Enemy e, int playerX, int playerY, IReadOnlyList<Enemy> enemies, Func<int, int, bool> isWalkable)
        {
            int dx = playerX - e.TileX, dy = playerY - e.TileY;
            int sx = Math.Sign(dx), sy = Math.Sign(dy);
            var options = new List<int[]>();
            if (sx != 0 && sy != 0)
            {
                options.Add(new[] { sx, sy });
                if (Math.Abs(dx) >= Math.Abs(dy)) { options.Add(new[] { sx, 0 }); options.Add(new[] { 0, sy }); }
                else { options.Add(new[] { 0, sy }); options.Add(new[] { sx, 0 }); }
            }
            else if (sx != 0)
            {
                options.Add(new[] { sx, 0 }); options.Add(new[] { sx, 1 }); options.Add(new[] { sx, -1 });
            }
            else
            {
                options.Add(new[] { 0, sy }); options.Add(new[] { 1, sy }); options.Add(new[] { -1, sy });
            }
            foreach (var o in options)
            {
                int nx = e.TileX + o[0], ny = e.TileY + o[1];
                if (isWalkable != null)
                {
                    if (!isWalkable(nx, ny)) continue;
                    if (o[0] != 0 && o[1] != 0 && (!isWalkable(e.TileX + o[0], e.TileY) || !isWalkable(e.TileX, e.TileY + o[1]))) continue;
                }
                if (TileOccupancy.TryMoveEnemy(e, nx, ny, playerX, playerY, enemies)) return true;
            }
            return false;
        }

        /// <summary>The ground items of the map the player is on (#36, #62). Kills put their drop here; <see cref="Character.EnterMap"/> switches it per map. Never null.</summary>
        public GroundItems Ground { get; internal set; } = new GroundItems();

        private CombatResult Strike(Enemy target, int damage)
        {
            int dealt = target.TakeDamage(damage);
            MarkInCombat();
            if (!target.IsDead) return new CombatResult(CombatOutcome.Hit, target, dealt);
            bool xpBlocked = target.XpReward > 0 && !_owner.EarnsKillXp;          // #35: Death debuff 3
            int xp = xpBlocked ? 0 : target.XpReward;
            int levels = xp > 0 ? _owner.GainXp(xp) : 0;                        // #36
            var drop = target.DropItem;
            var ground = drop == null ? null : Ground.Add(drop, 1, target.TileX, target.TileY);   // left where it fell; the player picks it up
            var completed = _owner.Quests.RecordKill(target.Type);
            return new CombatResult(CombatOutcome.Killed, target, dealt, completed, xpGained: xp, levelsGained: levels, dropped: drop, groundItem: ground, xpBlocked: xpBlocked);
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
