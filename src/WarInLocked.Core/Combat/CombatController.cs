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
        /// <summary>The ability costs more energy than the character has.</summary>
        NotEnoughEnergy,
        /// <summary>The hotbar slot is empty.</summary>
        EmptySlot,
        /// <summary>The target was hit and survived.</summary>
        Hit,
        /// <summary>The target was hit and died; quests were told (see <see cref="CombatResult.CompletedQuests"/>).</summary>
        Killed,
    }

    public sealed class CombatResult
    {
        private static readonly Quest[] None = new Quest[0];

        public CombatResult(CombatOutcome outcome, Enemy target = null, int damage = 0, IReadOnlyList<Quest> completedQuests = null)
        {
            Outcome = outcome;
            Target = target;
            Damage = damage;
            CompletedQuests = completedQuests ?? None;
        }

        public CombatOutcome Outcome { get; }
        public Enemy Target { get; }
        public int Damage { get; }
        /// <summary>Quests that finished because of this kill (rewards already granted).</summary>
        public IReadOnlyList<Quest> CompletedQuests { get; }
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

        /// <summary>Basic attack damage: (10 + weapon bonus) x Strength modifier, rounded half up; unarmed is 10 x the modifier. See <see cref="DamageCalculator"/>.</summary>
        public static int BasicAttackDamage(Character character) => DamageCalculator.PhysicalDamage(character);

        /// <summary>Damage of an ability: the physical damage times its multiplier (Slash 1.5x), rounded half up once at the end.</summary>
        public static int AbilityDamage(Character character, AbilityDefinition ability) =>
            DamageCalculator.PhysicalDamage(character, DamageCalculator.WeaponBonus(character), ability.DamageMultiplier);

        /// <summary>Advances cooldowns, regenerates energy and puts newly learned abilities on the hotbar.</summary>
        public void Update(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (AttackCooldownRemaining > 0) AttackCooldownRemaining = Math.Max(0, AttackCooldownRemaining - seconds);
            Hotbar.Update(seconds);
            _owner.RegenEnergy(EnergyRegenPerSecond * seconds);
            SyncHotbar();
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
            return Strike(target, BasicAttackDamage(_owner));
        }

        /// <summary>Keys 1-5 (index 0-4): uses the ability in that hotbar slot.</summary>
        public CombatResult UseHotbar(int slotIndex, int playerX, int playerY, IReadOnlyList<Enemy> enemies)
        {
            SyncHotbar();
            var slot = Hotbar.Get(slotIndex);
            var ability = slot.IsEmpty ? null : AbilityLibrary.Get(slot.AbilityId);
            if (ability == null) return new CombatResult(CombatOutcome.EmptySlot);
            if (!AttackReady || !slot.IsReady) return new CombatResult(CombatOutcome.OnCooldown);
            if (_owner.Energy < ability.EnergyCost) return new CombatResult(CombatOutcome.NotEnoughEnergy);
            var target = FindTarget(playerX, playerY, enemies);
            if (target == null) return new CombatResult(CombatOutcome.NoTarget); // nothing is spent without a target

            _owner.SpendEnergy(ability.EnergyCost);
            Hotbar.StartCooldown(slotIndex, ability.CooldownSeconds);
            AttackCooldownRemaining = AttackCooldownSeconds;
            return Strike(target, AbilityDamage(_owner, ability));
        }

        private CombatResult Strike(Enemy target, int damage)
        {
            int dealt = target.TakeDamage(damage);
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
