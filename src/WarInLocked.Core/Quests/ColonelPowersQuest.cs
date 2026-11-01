using System.Collections.Generic;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Quests
{
    /// <summary>
    /// Colonel Powers and the tutorial quest (issue #8): which dialogue he speaks, when the quest prompt opens,
    /// and what accepting does (starts the quest and gives the Practice Sword, once).
    /// </summary>
    public static class ColonelPowersQuest
    {
        public const string SwordGivenFlag = "practice-sword-given";
        public const string SwordNotice = "Practice sword given";

        /// <summary>Which dialogue the Colonel starts with for this character right now.</summary>
        public static string DialogueIdFor(Character character)
        {
            var q1 = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (q1 == null) return DialogueLibrary.ColonelPowersId;           // first meeting: the full intro
            switch (q1.State)
            {
                case QuestState.Offered: return DialogueLibrary.ColonelPowersReofferId;
                case QuestState.Active: return DialogueLibrary.ColonelPowersActiveId;
                case QuestState.ReadyToTurnIn: return DialogueLibrary.ColonelPowersTurnInId;
            }
            var q2 = character.Quests.Get(QuestLibrary.SlashingPracticeId);
            if (q2 == null) return DialogueLibrary.ColonelPowersPracticeOfferId;
            switch (q2.State)
            {
                case QuestState.Offered: return DialogueLibrary.ColonelPowersPracticeOfferId;
                case QuestState.Active: return DialogueLibrary.ColonelPowersPracticeActiveId;
                case QuestState.ReadyToTurnIn: return DialogueLibrary.ColonelPowersPracticeTurnInId;
                default: return DialogueLibrary.ColonelPowersDoneId;
            }
        }

        public static DialogueRunner CreateDialogue(Character character)
        {
            var vars = DialogueRunner.VariablesFor(character);
            var quest = CurrentQuest(character);
            if (quest != null && quest.Objectives.Count > 0)
            {
                string current = quest.Objectives[0].Current.ToString();
                vars["slain"] = current;
                vars["used"] = current;
                vars["required"] = quest.Objectives[0].Required.ToString();
            }
            return new DialogueRunner(DialogueLibrary.Get(DialogueIdFor(character)), vars);
        }

        /// <summary>The Colonel's quest that is not complete yet (quest 1, then quest 2), or the last one when all are done.</summary>
        private static Quest CurrentQuest(Character character)
        {
            var q1 = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (q1 == null || q1.State != QuestState.Complete) return q1;
            return character.Quests.Get(QuestLibrary.SlashingPracticeId) ?? q1;
        }

        /// <summary>
        /// Call when a Colonel Powers dialogue has ended. First turns in a finished quest (reward: XP and abilities, once), then returns
        /// the quest to show in the prompt (Accept Yes/No), or null if there is nothing to offer.
        /// Order of events: intro, then "Slice and slime" is offered; its turn-in dialogue ends with the reward and "Slashing Practice" is offered.
        /// </summary>
        public static Quest QuestToOfferAfterDialogue(Character character) => QuestToOfferAfterDialogue(character, out _);

        /// <param name="notice">A short message when a quest was just turned in, e.g. "Quest complete: Slice and slime!  +50 XP, learned Slash", else null.</param>
        public static Quest QuestToOfferAfterDialogue(Character character, out string notice)
        {
            notice = TurnInReadyQuest(character);
            var q1 = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (q1 == null) return character.Quests.Offer(QuestLibrary.SliceAndSlime());
            if (q1.State == QuestState.Offered) return q1;
            if (q1.State != QuestState.Complete) return null;
            var q2 = character.Quests.Get(QuestLibrary.SlashingPracticeId);
            if (q2 == null) return character.Quests.Offer(QuestLibrary.SlashingPractice());
            return q2.State == QuestState.Offered ? q2 : null;
        }

        /// <summary>Turns in whichever of the Colonel's quests is ReadyToTurnIn. Returns the notice, or null if nothing was ready.</summary>
        public static string TurnInReadyQuest(Character character)
        {
            foreach (string id in new[] { QuestLibrary.SliceAndSlimeId, QuestLibrary.SlashingPracticeId })
            {
                var q = character.Quests.Get(id);
                if (q == null || q.State != QuestState.ReadyToTurnIn) continue;
                character.Quests.TurnIn(id);
                var parts = new List<string>();
                if (q.Reward.Xp > 0) parts.Add("+" + q.Reward.Xp + " XP");
                foreach (var a in q.Reward.AbilityIds) parts.Add("learned " + AbilityLibrary.NameOf(a));
                return "Quest complete: " + q.ShortTitle + "!  " + string.Join(", ", parts);
            }
            return null;
        }

        /// <summary>
        /// The player pressed Yes. Starts the Offered quest. For "Slice and slime" it also hands over the Practice Sword (only the first time),
        /// equipped in the main hand; if the sword cannot be given (inventory full and hand busy) nothing happens and the quest stays Offered.
        /// For "Slashing Practice" it just starts the quest. <paramref name="notice"/> is the short message to show ("Practice sword given"), or null.
        /// </summary>
        public static bool Accept(Character character, out string notice)
        {
            notice = null;
            var q1 = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (q1 != null && q1.State == QuestState.Offered)
            {
                if (!character.HasFlag(SwordGivenFlag))
                {
                    var result = character.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
                    if (result != EquipResult.Success)
                    {
                        notice = "Your inventory is full";
                        return false;
                    }
                    character.SetFlag(SwordGivenFlag);
                    notice = SwordNotice;
                }
                return character.Quests.Accept(QuestLibrary.SliceAndSlimeId);
            }
            var q2 = character.Quests.Get(QuestLibrary.SlashingPracticeId);
            if (q2 != null && q2.State == QuestState.Offered) return character.Quests.Accept(QuestLibrary.SlashingPracticeId);
            return false;
        }
    }
}
