using System;
using System.Collections.Generic;
using System.Linq;

namespace WarInLocked.Core.Quests
{
    public enum QuestState { Offered, Active, Complete }

    /// <summary>"Kill N enemies of one type" (the enemy type is a plain string such as "slime").</summary>
    public sealed class QuestObjective
    {
        public QuestObjective(string enemyType, int required, string label)
        {
            if (required < 1) throw new ArgumentOutOfRangeException(nameof(required));
            EnemyType = enemyType;
            Required = required;
            Label = label;
        }

        public string EnemyType { get; }
        public int Required { get; }
        /// <summary>Shown to the player, e.g. "Slimes slain".</summary>
        public string Label { get; }
        public int Current { get; internal set; }
        public bool IsDone => Current >= Required;

        /// <summary>For example "Slimes slain 2/5".</summary>
        public string ProgressText => Label + " " + Current + "/" + Required;
    }

    public sealed class QuestReward
    {
        public QuestReward(int xp, IEnumerable<string> abilityIds)
        {
            Xp = xp;
            AbilityIds = (abilityIds ?? Enumerable.Empty<string>()).ToArray();
        }

        public int Xp { get; }
        public IReadOnlyList<string> AbilityIds { get; }

        /// <summary>For example "50 XP, Ability Slash".</summary>
        public string Text
        {
            get
            {
                var parts = new List<string>();
                if (Xp > 0) parts.Add(Xp + " XP");
                foreach (var id in AbilityIds) parts.Add("Ability " + AbilityLibrary.NameOf(id));
                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>A quest and its progress. Owned by a <see cref="QuestLog"/>; state changes go through the log.</summary>
    public sealed class Quest
    {
        public Quest(string id, string title, string description, IEnumerable<QuestObjective> objectives, QuestReward reward, string failureCondition = "None")
        {
            Id = id;
            Title = title;
            Description = description;
            Objectives = objectives.ToArray();
            Reward = reward;
            FailureCondition = failureCondition;
            State = QuestState.Offered;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<QuestObjective> Objectives { get; }
        public QuestReward Reward { get; }
        public string FailureCondition { get; }
        public QuestState State { get; internal set; }

        public bool AllObjectivesDone => Objectives.All(o => o.IsDone);

        /// <summary>One line such as "Slimes slain 0/5" (several objectives are joined with ", ").</summary>
        public string ProgressText => string.Join(", ", Objectives.Select(o => o.ProgressText));
    }
}
