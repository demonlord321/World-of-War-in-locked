using WarInLocked.Core.Characters;
using WarInLocked.Core.Dialogue;

namespace WarInLocked.Core.Quests
{
    /// <summary>
    /// Whiplash in the Adventurers encampment (#52): which dialogue he speaks and what finishing it does. Same pattern as
    /// <see cref="ColonelPowersQuest"/>: build the dialogue with <see cref="CreateDialogue"/>, and when it has ended call <see cref="AfterDialogue"/>.
    /// </summary>
    public static class WhiplashQuest
    {
        /// <summary>The NPC id and the map data name for Whiplash.</summary>
        public const string NpcId = QuestLibrary.WhiplashNpcId;

        /// <summary>His dialogue now: the advice while "Adventurers start" is active, otherwise a short idle line.</summary>
        public static string DialogueIdFor(Character character)
        {
            var q = character.Quests.Get(QuestLibrary.AdventurersStartId);
            return q != null && q.State == QuestState.Active ? DialogueLibrary.WhiplashId : DialogueLibrary.WhiplashIdleId;
        }

        public static DialogueRunner CreateDialogue(Character character) =>
            new DialogueRunner(DialogueLibrary.Get(DialogueIdFor(character)), character);

        /// <summary>
        /// Call when a Whiplash dialogue has ended (or at the start of the talk). Counts the conversation toward "Adventurers start": that quest completes
        /// and its reward (50 XP, 50 copper) is given. <paramref name="notice"/> is "Quest complete: Adventurers start!  +50 XP, +50 copper", or null if
        /// nothing completed (quest not accepted, or already done). Returns true when a quest was completed.
        /// </summary>
        public static bool AfterDialogue(Character character, out string notice)
        {
            notice = null;
            foreach (var q in character.Quests.NotifyTalkedTo(NpcId))
                if (q.State == QuestState.Complete) notice = QuestNotices.Complete(q);
            return notice != null;
        }
    }
}
