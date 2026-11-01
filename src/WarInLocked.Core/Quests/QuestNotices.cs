using System.Collections.Generic;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Quests
{
    /// <summary>The short message shown when a quest is completed, shared by every quest giver.</summary>
    public static class QuestNotices
    {
        /// <summary>For example "Quest complete: Adventurers start!  +50 XP, +50 copper".</summary>
        public static string Complete(Quest q)
        {
            var parts = new List<string>();
            foreach (var t in q.Reward.TitleIds) parts.Add("title " + (TitleLibrary.Get(t)?.Name ?? t));
            if (q.Reward.Xp > 0) parts.Add("+" + q.Reward.Xp + " XP");
            if (q.Reward.Copper > 0) parts.Add("+" + q.Reward.Copper + " copper");
            foreach (var a in q.Reward.AbilityIds) parts.Add("learned " + AbilityLibrary.NameOf(a));
            foreach (var i in q.Reward.ItemIds) parts.Add(ItemLibrary.Get(i)?.Name ?? i);
            return "Quest complete: " + q.ShortTitle + "!  " + string.Join(", ", parts);
        }
    }
}
