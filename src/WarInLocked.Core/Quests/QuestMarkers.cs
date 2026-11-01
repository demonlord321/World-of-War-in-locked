using System;

namespace WarInLocked.Core.Quests
{
    /// <summary>The marker drawn above a quest NPC (Dyllan, 1 Nov).</summary>
    public enum QuestMarker
    {
        /// <summary>Nothing to show.</summary>
        None,
        /// <summary>Yellow "!": a quest can be taken from this NPC.</summary>
        Available,
        /// <summary>Grey/black "?": the player has an unfinished quest from this NPC (or one to hand in to this NPC).</summary>
        InProgress,
        /// <summary>Yellow "?": a quest can be handed in to this NPC (or, for "Adventurers start", finished by talking to him).</summary>
        ReadyToTurnIn,
    }

    /// <summary>
    /// Which marker an NPC shows for a character. Mapmaker draws from <c>QuestMarkers.MarkerFor(npcId, character)</c> (or <c>character.Quests.MarkerFor(npcId)</c>)
    /// each frame, with the NPC ids of the map data ("colonel-powers", "whiplash"). Priority when several apply: ReadyToTurnIn, then Available, then InProgress.
    /// </summary>
    public static class QuestMarkers
    {
        public static QuestMarker MarkerFor(string npcId, Characters.Character character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (string.IsNullOrEmpty(npcId)) return QuestMarker.None;
            var log = character.Quests;

            bool inProgress = false;
            foreach (var q in log.All)
            {
                bool turnIn = QuestLibrary.TurnInNpcId(q.Id) == npcId;
                bool giver = QuestLibrary.GiverNpcId(q.Id) == npcId;
                if (q.State == QuestState.ReadyToTurnIn && turnIn) return QuestMarker.ReadyToTurnIn;
                // A quest that completes by talking ("Adventurers start") is as good as ready while the only thing left is a chat with this NPC.
                if (q.State == QuestState.Active && turnIn && FinishedByTalkingTo(q, npcId)) return QuestMarker.ReadyToTurnIn;
                if ((q.State == QuestState.Active || q.State == QuestState.ReadyToTurnIn) && (giver || turnIn)) inProgress = true;
            }

            if (HasQuestToOffer(npcId, log)) return QuestMarker.Available;
            return inProgress ? QuestMarker.InProgress : QuestMarker.None;
        }

        // The quest completes on its objectives and everything still open is a "talk to this NPC" objective.
        private static bool FinishedByTalkingTo(Quest q, string npcId)
        {
            if (!q.CompletesOnObjectives) return false;
            bool anyOpen = false;
            foreach (var o in q.Objectives)
            {
                if (o.IsDone) continue;
                if (o.Kind != ObjectiveKind.Talk || o.NpcId != npcId) return false;
                anyOpen = true;
            }
            return anyOpen;
        }

        // A quest this NPC gives that the player has not accepted yet: shown but declined (Offered), or not offered yet with its prerequisite Complete.
        private static bool HasQuestToOffer(string npcId, QuestLog log)
        {
            foreach (string id in QuestLibrary.StoryOrder)
            {
                if (QuestLibrary.GiverNpcId(id) != npcId) continue;
                var q = log.Get(id);
                if (q != null)
                {
                    if (q.State == QuestState.Offered) return true;
                    continue;
                }
                string pre = QuestLibrary.PrerequisiteId(id);
                if (pre == null || log.Get(pre)?.State == QuestState.Complete) return true;
            }
            return false;
        }
    }
}
