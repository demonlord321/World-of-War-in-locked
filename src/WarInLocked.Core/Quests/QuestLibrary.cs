namespace WarInLocked.Core.Quests
{
    public static class QuestLibrary
    {
        public const string SliceAndSlimeId = "slice-and-slime";

        /// <summary>The tutorial quest (issue #8), Dyllan's text. A fresh object each call.</summary>
        public static Quest SliceAndSlime() => new Quest(
            SliceAndSlimeId,
            "Quest: Slice and slime.",
            "Colonel Powers has requested you hunt down 5 of the slimes in the area.",
            new[] { new QuestObjective("slime", 5, "Slimes slain") },
            new QuestReward(50, new[] { AbilityLibrary.Slash }),
            "None");

        /// <summary>Short name for the HUD tracker: "Slice and slime".</summary>
        public const string SliceAndSlimeShortTitle = "Slice and slime";
    }
}
