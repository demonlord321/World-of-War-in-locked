using System;
using System.Collections.Generic;
using System.Linq;
using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Quests
{
    /// <summary>The quests a character has been offered, is doing or has finished. Lives on <see cref="Character.Quests"/>.</summary>
    public sealed class QuestLog
    {
        private readonly Character _owner;
        private readonly List<Quest> _quests = new List<Quest>();

        public QuestLog(Character owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public event EventHandler Changed;

        public IReadOnlyList<Quest> All => _quests.ToArray();
        /// <summary>Quests being done or waiting to be turned in (what the HUD tracker shows).</summary>
        public IReadOnlyList<Quest> Active => _quests.Where(q => q.State == QuestState.Active || q.State == QuestState.ReadyToTurnIn).ToArray();

        public Quest Get(string id) => _quests.FirstOrDefault(q => q.Id == id);

        /// <summary>Puts a quest in the log in the Offered state. Offering one that is already there returns the existing one.</summary>
        public Quest Offer(Quest quest)
        {
            var existing = Get(quest.Id);
            if (existing != null) return existing;
            quest.State = QuestState.Offered;
            _quests.Add(quest);
            Changed?.Invoke(this, EventArgs.Empty);
            return quest;
        }

        /// <summary>Offered to Active. Returns false if the quest is unknown or not Offered.</summary>
        public bool Accept(string id)
        {
            var q = Get(id);
            if (q == null || q.State != QuestState.Offered) return false;
            q.State = QuestState.Active;
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Counts one defeated enemy toward every Active quest that wants that type. A quest whose objectives are all done becomes
        /// ReadyToTurnIn (no reward yet, see <see cref="TurnIn"/>). Returns the quests that became ready because of this kill.
        /// </summary>
        public IReadOnlyList<Quest> RecordKill(string enemyType) =>
            Record(o => o.Kind == ObjectiveKind.Kill && o.EnemyType == enemyType);

        /// <summary>Counts one cast of an ability that actually fired. Same rules as <see cref="RecordKill"/> for "Use Slash N times" quests.</summary>
        public IReadOnlyList<Quest> RecordAbilityUse(string abilityId) =>
            Record(o => o.Kind == ObjectiveKind.UseAbility && o.AbilityId == abilityId);

        private IReadOnlyList<Quest> Record(Func<QuestObjective, bool> matches)
        {
            var ready = new List<Quest>();
            bool changed = false;
            foreach (var q in _quests)
            {
                if (q.State != QuestState.Active) continue;
                foreach (var o in q.Objectives)
                {
                    if (!matches(o) || o.IsDone) continue;
                    o.Current++;
                    changed = true;
                }
                if (q.AllObjectivesDone)
                {
                    q.State = QuestState.ReadyToTurnIn;
                    ready.Add(q);
                }
            }
            if (changed) Changed?.Invoke(this, EventArgs.Empty);
            return ready;
        }

        /// <summary>
        /// ReadyToTurnIn to Complete: gives the reward (XP, abilities) to the character, once. Returns false if the quest is
        /// unknown or not ready.
        /// </summary>
        public bool TurnIn(string id)
        {
            var q = Get(id);
            if (q == null || q.State != QuestState.ReadyToTurnIn) return false;
            q.State = QuestState.Complete;
            _owner.GainXp(q.Reward.Xp);
            foreach (var a in q.Reward.AbilityIds) _owner.LearnAbility(a);
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
