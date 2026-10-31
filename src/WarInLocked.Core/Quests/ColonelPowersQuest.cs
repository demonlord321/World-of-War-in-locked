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
            var quest = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (quest == null) return DialogueLibrary.ColonelPowersId;           // first meeting: the full intro
            switch (quest.State)
            {
                case QuestState.Offered: return DialogueLibrary.ColonelPowersReofferId;
                case QuestState.Active: return DialogueLibrary.ColonelPowersActiveId;
                default: return DialogueLibrary.ColonelPowersDoneId;
            }
        }

        public static DialogueRunner CreateDialogue(Character character)
        {
            var vars = DialogueRunner.VariablesFor(character);
            var quest = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (quest != null && quest.Objectives.Count > 0)
            {
                vars["slain"] = quest.Objectives[0].Current.ToString();
                vars["required"] = quest.Objectives[0].Required.ToString();
            }
            return new DialogueRunner(DialogueLibrary.Get(DialogueIdFor(character)), vars);
        }

        /// <summary>
        /// Call when a Colonel Powers dialogue has ended. Returns the quest to show in the prompt (Accept Yes/No),
        /// or null if there is nothing to offer (quest already active or complete).
        /// The intro dialogue only ends after the "My name is..." path, so ending it means the intro is done.
        /// </summary>
        public static Quest QuestToOfferAfterDialogue(Character character)
        {
            var quest = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (quest == null) return character.Quests.Offer(QuestLibrary.SliceAndSlime());
            return quest.State == QuestState.Offered ? quest : null;
        }

        /// <summary>
        /// The player pressed Yes. Starts the quest and hands over the Practice Sword (only the first time), equipped in the main hand.
        /// If the sword cannot be given (inventory full and hand busy) nothing happens and the quest stays Offered.
        /// <paramref name="notice"/> is the short message to show ("Practice sword given"), or null.
        /// </summary>
        public static bool Accept(Character character, out string notice)
        {
            notice = null;
            var quest = character.Quests.Get(QuestLibrary.SliceAndSlimeId);
            if (quest == null || quest.State != QuestState.Offered) return false;

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
    }
}
