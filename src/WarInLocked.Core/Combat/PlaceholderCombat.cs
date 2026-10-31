using System.Collections.Generic;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Interaction;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;

namespace WarInLocked.Core.Combat
{
    public enum AttackOutcome
    {
        /// <summary>Nothing equipped in the main hand ("You need a weapon").</summary>
        NoWeapon,
        /// <summary>No enemy on a neighbouring tile.</summary>
        NoTarget,
        /// <summary>An enemy was defeated; see <see cref="AttackResult.TargetIndex"/>.</summary>
        Defeated,
    }

    public sealed class AttackResult
    {
        public AttackResult(AttackOutcome outcome, int targetIndex, IReadOnlyList<Quest> completedQuests)
        {
            Outcome = outcome;
            TargetIndex = targetIndex;
            CompletedQuests = completedQuests;
        }

        public AttackOutcome Outcome { get; }
        /// <summary>Index into the enemy list passed to Attack, or -1.</summary>
        public int TargetIndex { get; }
        public IReadOnlyList<Quest> CompletedQuests { get; }
    }

    /// <summary>
    /// PLACEHOLDER for real combat (see QUESTIONS.md): with a weapon in the main hand, one adjacent enemy (8-way) is defeated
    /// instantly and counted toward the character's quests. Replace this when real combat exists.
    /// </summary>
    public static class PlaceholderCombat
    {
        public const string NeedWeaponText = "You need a weapon";

        public static bool HasWeapon(Character character)
        {
            var w = character.Equipment.Get(EquipmentSlot.MainHand);
            return w != null && w.Type == ItemType.Weapon;
        }

        public static AttackResult Attack(Character character, int playerX, int playerY, IReadOnlyList<(int X, int Y)> enemyTiles, string enemyType)
        {
            if (!HasWeapon(character)) return new AttackResult(AttackOutcome.NoWeapon, -1, new Quest[0]);

            int best = -1;
            bool bestStraight = false;
            for (int i = 0; i < enemyTiles.Count; i++)
            {
                var e = enemyTiles[i];
                if (!TalkTarget.IsAdjacent(playerX, playerY, e.X, e.Y)) continue;
                bool straight = e.X == playerX || e.Y == playerY;
                if (best < 0 || (straight && !bestStraight)) { best = i; bestStraight = straight; }
            }
            if (best < 0) return new AttackResult(AttackOutcome.NoTarget, -1, new Quest[0]);

            return new AttackResult(AttackOutcome.Defeated, best, character.Quests.RecordKill(enemyType));
        }
    }
}
