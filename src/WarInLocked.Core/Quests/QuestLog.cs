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
        public IReadOnlyList<Quest> Active => _quests.Where(q => q.State == QuestState.Active).ToArray();

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
        /// Complete and its reward (XP, abilities) is given to the character. Returns the quests completed by this kill.
        /// </summary>
        public IReadOnlyList<Quest> RecordKill(string enemyType)
        {
            var completed = new List<Quest>();
            bool changed = false;
            foreach (var q in _quests)
            {
                if (q.State != QuestState.Active) continue;
                foreach (var o in q.Objectives)
                {
                    if (o.EnemyType != enemyType || o.IsDone) continue;
                    o.Current++;
                    changed = true;
                }
                if (q.AllObjectivesDone)
                {
                    q.State = QuestState.Complete;
                    _owner.GainXp(q.Reward.Xp);
                    foreach (var a in q.Reward.AbilityIds) _owner.LearnAbility(a);
                    completed.Add(q);
                }
            }
            if (changed) Changed?.Invoke(this, EventArgs.Empty);
            return completed;
        }
    }
}
