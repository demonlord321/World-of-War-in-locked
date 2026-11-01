namespace WarInLocked.Core.Quests
{
    public static class QuestLibrary
    {
        public const string SliceAndSlimeId = "slice-and-slime";
        public const string SlashingPracticeId = "slashing-practice";

        /// <summary>The tutorial quest (issue #8), Dyllan's text. A fresh object each call. Reward is given when turned in to the Colonel (#19).</summary>
        public static Quest SliceAndSlime() => new Quest(
            SliceAndSlimeId,
            "Quest: Slice and slime.",
            "Colonel Powers has requested you hunt down 5 of the slimes in the area.",
            new[] { new QuestObjective("slime", 5, "Slimes slain") },
            new QuestReward(50, new[] { AbilityLibrary.Slash }),
            "None",
            SliceAndSlimeShortTitle);

        /// <summary>Tutorial quest 2 (#19): use Slash 5 times on enemies. Counts Slash casts that hit an enemy.</summary>
        public static Quest SlashingPractice() => new Quest(
            SlashingPracticeId,
            "Quest: Slashing Practice.",
            "Use Slash 5 times on enemies.",
            new[] { QuestObjective.UseAbility(AbilityLibrary.Slash, 5, "Slash used") },
            new QuestReward(50, new[] { AbilityLibrary.EnergyBall }),
            "None",
            SlashingPracticeShortTitle);

        /// <summary>Short name for the HUD tracker: "Slice and slime".</summary>
        public const string SliceAndSlimeShortTitle = "Slice and slime";
        public const string SlashingPracticeShortTitle = "Slashing Practice";
    }
}
