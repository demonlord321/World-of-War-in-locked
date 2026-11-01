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
        public const string StaffGivenFlag = "practice-staff-given";
        public const string StaffNotice = "Practice staff given";
        public const string InventoryFullNotice = "Your inventory is full";

        // The Colonel's quests in order: quest id, dialogue when offered-but-not-accepted, while active, and when ready to turn in.
        private static readonly string[][] Chain =
        {
            new[] { QuestLibrary.SliceAndSlimeId, DialogueLibrary.ColonelPowersReofferId, DialogueLibrary.ColonelPowersActiveId, DialogueLibrary.ColonelPowersTurnInId },
            new[] { QuestLibrary.SlashingPracticeId, DialogueLibrary.ColonelPowersPracticeOfferId, DialogueLibrary.ColonelPowersPracticeActiveId, DialogueLibrary.ColonelPowersPracticeTurnInId },
            new[] { QuestLibrary.EnergyBallPracticeId, DialogueLibrary.ColonelPowersEnergyOfferId, DialogueLibrary.ColonelPowersEnergyActiveId, DialogueLibrary.ColonelPowersEnergyTurnInId },
        };

        private static Quest NewQuest(string id)
        {
            switch (id)
            {
                case QuestLibrary.SliceAndSlimeId: return QuestLibrary.SliceAndSlime();
                case QuestLibrary.SlashingPracticeId: return QuestLibrary.SlashingPractice();
                default: return QuestLibrary.EnergyBallPractice();
            }
        }

        /// <summary>Which dialogue the Colonel starts with for this character right now.</summary>
        public static string DialogueIdFor(Character character)
        {
            for (int i = 0; i < Chain.Length; i++)
            {
                var q = character.Quests.Get(Chain[i][0]);
                if (q == null) return i == 0 ? DialogueLibrary.ColonelPowersId : Chain[i][1];   // first meeting: the full intro
                switch (q.State)
                {
                    case QuestState.Offered: return Chain[i][1];
                    case QuestState.Active: return Chain[i][2];
                    case QuestState.ReadyToTurnIn: return Chain[i][3];
                }
            }
            return DialogueLibrary.ColonelPowersDoneId;
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

        /// <summary>The Colonel's first quest that is not complete yet, or the last one when all are done.</summary>
        private static Quest CurrentQuest(Character character)
        {
            Quest last = null;
            foreach (var step in Chain)
            {
                last = character.Quests.Get(step[0]);
                if (last == null || last.State != QuestState.Complete) return last;
            }
            return last;
        }

        /// <summary>
        /// Call when a Colonel Powers dialogue has ended. First turns in a finished quest (reward: XP, copper and abilities, once), then returns
        /// the quest to show in the prompt (Accept Yes/No), or null if there is nothing to offer.
        /// Order of events: intro, then "Slice and slime" is offered; turning it in offers "Slashing Practice"; turning that in offers "Energy Ball practice".
        /// </summary>
        public static Quest QuestToOfferAfterDialogue(Character character) => QuestToOfferAfterDialogue(character, out _);

        /// <param name="notice">A short message when a quest was just turned in, e.g. "Quest complete: Slice and slime!  +50 XP, learned Slash", else null.</param>
        public static Quest QuestToOfferAfterDialogue(Character character, out string notice)
        {
            notice = TurnInReadyQuest(character);
            foreach (var step in Chain)
            {
                var q = character.Quests.Get(step[0]);
                if (q == null) return character.Quests.Offer(NewQuest(step[0]));
                if (q.State == QuestState.Offered) return q;
                if (q.State != QuestState.Complete) return null;
            }
            return null;
        }

        /// <summary>Turns in whichever of the Colonel's quests is ReadyToTurnIn. Returns the notice, or null if nothing was ready.</summary>
        public static string TurnInReadyQuest(Character character)
        {
            foreach (var step in Chain)
            {
                var q = character.Quests.Get(step[0]);
                if (q == null || q.State != QuestState.ReadyToTurnIn) continue;
                character.Quests.TurnIn(q.Id);
                var parts = new List<string>();
                if (q.Reward.Xp > 0) parts.Add("+" + q.Reward.Xp + " XP");
                if (q.Reward.Copper > 0) parts.Add("+" + q.Reward.Copper + " copper");
                foreach (var a in q.Reward.AbilityIds) parts.Add("learned " + AbilityLibrary.NameOf(a));
                return "Quest complete: " + q.ShortTitle + "!  " + string.Join(", ", parts);
            }
            return null;
        }

        /// <summary>
        /// The player pressed Yes. Starts the Offered quest. Accepting "Slice and slime" also hands over the Practice Sword (only the first time),
        /// equipped in the main hand. Accepting "Energy Ball practice" puts the Practice Staff in the inventory (only the first time; it is not equipped).
        /// If the item cannot be given (inventory full), nothing happens, the quest stays Offered, and <paramref name="notice"/> is "Your inventory is full":
        /// the player makes room and talks to the Colonel again to be re-offered the quest.
        /// <paramref name="notice"/> is otherwise the short message to show ("Practice sword given"), or null.
        /// </summary>
        public static bool Accept(Character character, out string notice)
        {
            notice = null;
            foreach (var step in Chain)
            {
                var q = character.Quests.Get(step[0]);
                if (q == null || q.State != QuestState.Offered) continue;
                if (q.Id == QuestLibrary.SliceAndSlimeId && !character.HasFlag(SwordGivenFlag))
                {
                    if (character.Equipment.AddAndEquip(ItemLibrary.PracticeSword) != EquipResult.Success)
                    {
                        notice = InventoryFullNotice;
                        return false;
                    }
                    character.SetFlag(SwordGivenFlag);
                    notice = SwordNotice;
                }
                else if (q.Id == QuestLibrary.EnergyBallPracticeId && !character.HasFlag(StaffGivenFlag))
                {
                    if (character.Inventory.Add(ItemLibrary.PracticeStaff) < 1)
                    {
                        notice = InventoryFullNotice;
                        return false;
                    }
                    character.SetFlag(StaffGivenFlag);
                    notice = StaffNotice;
                }
                return character.Quests.Accept(q.Id);
            }
            return false;
        }
    }
}
