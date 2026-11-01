namespace WarInLocked.Core.Quests
{
    public static class QuestLibrary
    {
        public const string SliceAndSlimeId = "slice-and-slime";
        public const string SlashingPracticeId = "slashing-practice";
        public const string EnergyBallPracticeId = "energy-ball-practice";
        public const string ShoppingBeforeAdventureId = "shopping-before-adventure";
        public const string AdventurersStartId = "adventurers-start";
        /// <summary>The NPC id of Whiplash (map data and dialogue), who finishes "Adventurers start". Mapmaker places him in the Adventurers encampment (#53).</summary>
        public const string WhiplashNpcId = "whiplash";

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

        /// <summary>Tutorial quest 3 (#21): cast Energy Ball 5 times on enemies (casts that fire count). Reward 50 XP and 50 copper.</summary>
        public static Quest EnergyBallPractice() => new Quest(
            EnergyBallPracticeId,
            "Quest: Energy Ball practice.",
            "You have been asked to perform Energy Ball five times. Equip your staff and get to casting.",
            new[] { QuestObjective.UseAbility(AbilityLibrary.EnergyBall, 5, "Energy ball") },
            new QuestReward(50, null, 50),
            "None",
            EnergyBallPracticeShortTitle);

        /// <summary>
        /// Tutorial quest 4 (#25), Dyllan's text (including "baught"): buy a Potion of lesser healing from the Colonel.
        /// Reward: title Adventurer, 50 XP and Adventurer's rations.
        /// </summary>
        public static Quest ShoppingBeforeAdventure() => new Quest(
            ShoppingBeforeAdventureId,
            "Quest: Shopping before adventure.",
            "Colonel Powers has a potion of lesser healing he is willing to sell to you.",
            new[] { QuestObjective.Buy(Items.ItemLibrary.LesserHealingPotionId, 1, "Potion of lesser healing baught") },
            new QuestReward(50, null, 0, new[] { Characters.TitleLibrary.AdventurerId }, new[] { Items.ItemLibrary.AdventurersRationsId }),
            "None",
            ShoppingBeforeAdventureShortTitle);

        /// <summary>
        /// Tutorial quest 5 (#52), Dyllan's text: speak with Whiplash in the Adventurers encampment. Reward 50 XP and 50 copper, no failure.
        /// Talking to Whiplash completes it and pays out on the spot (placeholder, see QUESTIONS.md); the Colonel does not take it in.
        /// </summary>
        public static Quest AdventurersStart() => new Quest(
            AdventurersStartId,
            "Quest: Adventurers start.",
            "Enter the Adventurers camp and speak with Whiplash",
            new[] { QuestObjective.TalkTo(WhiplashNpcId, "Whiplash spoken with") },
            new QuestReward(50, null, 50),
            "None",
            AdventurersStartShortTitle,
            "Whiplash",
            completesOnObjectives: true);

        /// <summary>A fresh copy of the quest with this id (state Offered, nothing done), or null when the id is unknown. Used when loading a save (#54).</summary>
        public static Quest Create(string id)
        {
            switch (id)
            {
                case SliceAndSlimeId: return SliceAndSlime();
                case SlashingPracticeId: return SlashingPractice();
                case EnergyBallPracticeId: return EnergyBallPractice();
                case ShoppingBeforeAdventureId: return ShoppingBeforeAdventure();
                case AdventurersStartId: return AdventurersStart();
                default: return null;
            }
        }

        /// <summary>Short name for the HUD tracker: "Slice and slime".</summary>
        public const string SliceAndSlimeShortTitle = "Slice and slime";
        public const string SlashingPracticeShortTitle = "Slashing Practice";
        public const string EnergyBallPracticeShortTitle = "Energy Ball practice";
        public const string ShoppingBeforeAdventureShortTitle = "Shopping before adventure";
        public const string AdventurersStartShortTitle = "Adventurers start";
    }
}
