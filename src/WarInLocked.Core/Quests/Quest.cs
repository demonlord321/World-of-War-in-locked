using System;
using System.Collections.Generic;
using System.Linq;

namespace WarInLocked.Core.Quests
{
    /// <summary>
    /// Offered (shown, not accepted), Active (being done), ReadyToTurnIn (all objectives done, go back to the quest giver;
    /// no reward yet) and Complete (turned in, reward given).
    /// </summary>
    public enum QuestState { Offered, Active, ReadyToTurnIn, Complete }

    /// <summary>What an objective counts: enemies of a type being defeated, or casts of an ability.</summary>
    public enum ObjectiveKind { Kill, UseAbility, Buy, Talk }

    /// <summary>"Kill N enemies of one type" (the enemy type is a plain string such as "slime") or "Use ability X N times".</summary>
    public sealed class QuestObjective
    {
        public QuestObjective(string enemyType, int required, string label) : this(ObjectiveKind.Kill, enemyType, null, required, label) { }

        private QuestObjective(ObjectiveKind kind, string enemyType, string abilityId, int required, string label)
        {
            if (required < 1) throw new ArgumentOutOfRangeException(nameof(required));
            Kind = kind;
            EnemyType = enemyType;
            AbilityId = abilityId;
            Required = required;
            Label = label;
        }

        /// <summary>An objective that counts casts of an ability that actually fired (hit an enemy), e.g. "Slash used 0/5".</summary>
        public static QuestObjective UseAbility(string abilityId, int required, string label) =>
            new QuestObjective(ObjectiveKind.UseAbility, null, abilityId, required, label);

        /// <summary>An objective that counts purchases of an item from a shop (#25), e.g. "Potion of lesser healing baught 0/1".</summary>
        public static QuestObjective Buy(string itemId, int required, string label) =>
            new QuestObjective(ObjectiveKind.Buy, null, null, required, label) { ItemId = itemId };

        /// <summary>An objective completed by talking to an NPC (#52), e.g. "Whiplash spoken with 0/1". The npc id is the one used in the map and dialogue data.</summary>
        public static QuestObjective TalkTo(string npcId, string label) =>
            new QuestObjective(ObjectiveKind.Talk, null, null, 1, label) { NpcId = npcId };

        public ObjectiveKind Kind { get; }
        /// <summary>The NPC id for a Talk objective (e.g. "whiplash"), otherwise null.</summary>
        public string NpcId { get; private set; }
        /// <summary>The item id for a Buy objective, otherwise null.</summary>
        public string ItemId { get; private set; }
        /// <summary>The enemy type for a Kill objective, otherwise null.</summary>
        public string EnemyType { get; }
        /// <summary>The ability id for a UseAbility objective, otherwise null.</summary>
        public string AbilityId { get; }
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
        public QuestReward(int xp, IEnumerable<string> abilityIds, int copper = 0, IEnumerable<string> titleIds = null, IEnumerable<string> itemIds = null)
        {
            TitleIds = (titleIds ?? Enumerable.Empty<string>()).ToArray();
            ItemIds = (itemIds ?? Enumerable.Empty<string>()).ToArray();
            Xp = xp;
            Copper = copper;
            AbilityIds = (abilityIds ?? Enumerable.Empty<string>()).ToArray();
        }

        public int Xp { get; }
        /// <summary>Copper coins (#21), 0 if none.</summary>
        public int Copper { get; }
        public IReadOnlyList<string> AbilityIds { get; }
        /// <summary>Titles earned (#25, #27). They are granted, not equipped.</summary>
        public IReadOnlyList<string> TitleIds { get; }
        /// <summary>Items put in the inventory (one each), #25.</summary>
        public IReadOnlyList<string> ItemIds { get; }

        /// <summary>For example "50 XP, Ability Slash", "50 XP, 50 Copper" or "Title Adventurer, 50 XP, Adventurer's rations".</summary>
        public string Text
        {
            get
            {
                var parts = new List<string>();
                foreach (var id in TitleIds) parts.Add("Title " + (Characters.TitleLibrary.Get(id)?.Name ?? id));
                if (Xp > 0) parts.Add(Xp + " XP");
                if (Copper > 0) parts.Add(Copper + " Copper");
                foreach (var id in AbilityIds) parts.Add("Ability " + AbilityLibrary.NameOf(id));
                foreach (var id in ItemIds) parts.Add(Items.ItemLibrary.Get(id)?.Name ?? id);
                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>A quest and its progress. Owned by a <see cref="QuestLog"/>; state changes go through the log.</summary>
    public sealed class Quest
    {
        public Quest(string id, string title, string description, IEnumerable<QuestObjective> objectives, QuestReward reward, string failureCondition = "None", string shortTitle = null, string turnInNpcName = "Colonel Powers", bool completesOnObjectives = false)
        {
            Id = id;
            Title = title;
            Description = description;
            Objectives = objectives.ToArray();
            Reward = reward;
            FailureCondition = failureCondition;
            ShortTitle = shortTitle ?? title;
            TurnInNpcName = turnInNpcName;
            CompletesOnObjectives = completesOnObjectives;
            State = QuestState.Offered;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<QuestObjective> Objectives { get; }
        public QuestReward Reward { get; }
        public string FailureCondition { get; }
        public QuestState State { get; internal set; }
        /// <summary>Short name for the HUD tracker, e.g. "Slice and slime".</summary>
        public string ShortTitle { get; }
        /// <summary>Who the quest is turned in to once every objective is done, e.g. "Colonel Powers".</summary>
        public string TurnInNpcName { get; }
        /// <summary>
        /// True when finishing the last objective completes the quest at once, with the reward given on the spot (no trip back to the quest giver).
        /// Used by "Adventurers start" (#52): talking to Whiplash pays out.
        /// </summary>
        public bool CompletesOnObjectives { get; }
        public bool IsReadyToTurnIn => State == QuestState.ReadyToTurnIn;

        public bool AllObjectivesDone => Objectives.All(o => o.IsDone);

        /// <summary>One line such as "Slimes slain 0/5" (several objectives are joined with ", ").</summary>
        public string ProgressText => string.Join(", ", Objectives.Select(o => o.ProgressText));
    }
}
