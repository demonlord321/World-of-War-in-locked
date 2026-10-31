using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Interaction
{
    /// <summary>Where an NPC stands on the tile grid, and which dialogue it speaks.</summary>
    public sealed class NpcSpot
    {
        public NpcSpot(string id, string dialogueId, int x, int y)
        {
            Id = id;
            DialogueId = dialogueId;
            X = x;
            Y = y;
        }

        public string Id { get; }
        public string DialogueId { get; }
        public int X { get; }
        public int Y { get; }
    }

    /// <summary>Works out which NPC (if any) the player can talk to right now (issue #6).</summary>
    public static class TalkTarget
    {
        /// <summary>
        /// True when the NPC is on a neighbouring tile, including diagonals
        /// (the player walks freely between tiles, so lining up exactly would be fiddly).
        /// Standing on the same tile does not count.
        /// </summary>
        public static bool IsAdjacent(int playerX, int playerY, int npcX, int npcY)
        {
            int dx = Math.Abs(playerX - npcX);
            int dy = Math.Abs(playerY - npcY);
            return dx <= 1 && dy <= 1 && (dx + dy) > 0;
        }

        /// <summary>The closest adjacent NPC (straight neighbours beat diagonal ones), or null.</summary>
        public static NpcSpot Find(int playerX, int playerY, IEnumerable<NpcSpot> npcs)
        {
            NpcSpot best = null;
            int bestScore = int.MaxValue;
            foreach (NpcSpot npc in npcs)
            {
                if (!IsAdjacent(playerX, playerY, npc.X, npc.Y)) continue;
                int score = Math.Abs(playerX - npc.X) + Math.Abs(playerY - npc.Y); // 1 = straight, 2 = diagonal
                if (score < bestScore)
                {
                    best = npc;
                    bestScore = score;
                }
            }
            return best;
        }
    }
}
