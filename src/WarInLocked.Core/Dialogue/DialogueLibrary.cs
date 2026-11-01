using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Dialogue
{
    /// <summary>All dialogues, found by id. NPCs refer to these ids in the map file.</summary>
    public static class DialogueLibrary
    {
        public const string ColonelPowersId = "colonel-powers";

        /// <summary>Shown when the intro is done but the quest has not been accepted yet (issue #8: talking again re-offers it).</summary>
        public const string ColonelPowersReofferId = "colonel-powers-reoffer";
        /// <summary>Shown while the tutorial quest is active.</summary>
        public const string ColonelPowersActiveId = "colonel-powers-active";
        /// <summary>Shown after the tutorial quest is complete.</summary>
        public const string ColonelPowersDoneId = "colonel-powers-done";

        /// <summary>Turn-in of quest 1 (#19): two lines, then the Colonel offers "Slashing Practice".</summary>
        public const string ColonelPowersTurnInId = "colonel-powers-turnin";
        /// <summary>Said when "Slashing Practice" was offered but not accepted yet.</summary>
        public const string ColonelPowersPracticeOfferId = "colonel-powers-practice-offer";
        /// <summary>Shown while "Slashing Practice" is active.</summary>
        public const string ColonelPowersPracticeActiveId = "colonel-powers-practice-active";
        /// <summary>Turn-in of "Slashing Practice" (#21): Dyllan's three-part speech, after which "Energy Ball practice" is offered.</summary>
        public const string ColonelPowersPracticeTurnInId = "colonel-powers-practice-turnin";
        /// <summary>Said when "Energy Ball practice" was offered but not accepted yet (repeats the speech).</summary>
        public const string ColonelPowersEnergyOfferId = "colonel-powers-energy-offer";
        /// <summary>Shown while "Energy Ball practice" is active.</summary>
        public const string ColonelPowersEnergyActiveId = "colonel-powers-energy-active";
        /// <summary>Turn-in of "Energy Ball practice". PLACEHOLDER text (Dyllan gave none).</summary>
        public const string ColonelPowersEnergyTurnInId = "colonel-powers-energy-turnin";

        /// <summary>Dyllan's quest 3 speech (#21), split into three boxes at his "..." pauses; joined with spaces it is his text verbatim.</summary>
        public const string EnergySpeech1 = "You are starting to learn, you might just survive this war torn world... Congrats on getting your first spell.";
        public const string EnergySpeech2 = "Now take this and show me you can use the wonderful art that is magic...";
        public const string EnergySpeech3 = "The energy is with in you, you have used it for your slash... Now manifest it as an attack.";

        public const string TurnInLine1 = "Not bad scrub, but you have much to learn. But do not worry, I shall train you, till you are ready...";
        public const string TurnInLine2 = "You now have your first ability, why not test it out...";

        /// <summary>Dyllan's line from issue #8, said right after the intro.</summary>
        public const string LackSkillLine =
            "I see you lack the skill to survive this world... Take this sword and hunt down 5 Slimes.";

        public static DialogueGraph Get(string id)
        {
            switch (id)
            {
                case ColonelPowersId: return ColonelPowers();
                case ColonelPowersReofferId: return Single("lack", LackSkillLine);
                case ColonelPowersActiveId: return Single("active", "Keep at it, {name}... Slimes slain {slain}/{required}.");
                case ColonelPowersDoneId: return Single("done", "Well done, {name}. You have learned to survive... for now.");
                case ColonelPowersTurnInId:
                    return new DialogueGraph("turnin1", new[]
                    {
                        new DialogueNode("turnin1", "Colonel Powers", TurnInLine1, nextId: "turnin2"),
                        new DialogueNode("turnin2", "Colonel Powers", TurnInLine2),
                    });
                case ColonelPowersPracticeOfferId: return Single("practice-offer", TurnInLine2);
                case ColonelPowersPracticeActiveId: return Single("practice-active", "Keep at it, {name}... Slash used {used}/{required}.");
                case ColonelPowersPracticeTurnInId:
                case ColonelPowersEnergyOfferId: return EnergySpeech(id == ColonelPowersPracticeTurnInId ? "practice-turnin" : "energy-offer");
                case ColonelPowersEnergyActiveId: return Single("energy-active", "Keep at it, {name}... Energy ball {used}/{required}.");
                case ColonelPowersEnergyTurnInId: return Single("energy-turnin", "Good, {name}. You have a feel for magic now... Take this for your trouble.");
            }
            throw new KeyNotFoundException("No dialogue with id '" + id + "'.");
        }

        public static bool Exists(string id) =>
            id == ColonelPowersId || id == ColonelPowersReofferId || id == ColonelPowersActiveId || id == ColonelPowersDoneId ||
            id == ColonelPowersTurnInId || id == ColonelPowersPracticeOfferId || id == ColonelPowersPracticeActiveId || id == ColonelPowersPracticeTurnInId ||
            id == ColonelPowersEnergyOfferId || id == ColonelPowersEnergyActiveId || id == ColonelPowersEnergyTurnInId;

        private static DialogueGraph EnergySpeech(string first) =>
            new DialogueGraph(first, new[]
            {
                new DialogueNode(first, "Colonel Powers", EnergySpeech1, nextId: "speech2"),
                new DialogueNode("speech2", "Colonel Powers", EnergySpeech2, nextId: "speech3"),
                new DialogueNode("speech3", "Colonel Powers", EnergySpeech3),
            });

        private static DialogueGraph Single(string nodeId, string text) =>
            new DialogueGraph(nodeId, new[] { new DialogueNode(nodeId, "Colonel Powers", text) });

        /// <summary>
        /// Colonel Powers in the Forest of Beginnings (issue #7), Dyllan's words.
        /// The three player options return after option 2 or 3's reply (the reply stays on screen above them);
        /// option 1 leads to the closing line and then the dialogue ends.
        /// </summary>
        public static DialogueGraph ColonelPowers()
        {
            const string speaker = "Colonel Powers";

            DialogueOption[] ThreeOptions() => new[]
            {
                new DialogueOption("My name is {name}", "introduce"),
                new DialogueOption("Who the hell are you...", "who"),
                new DialogueOption("Where the hell am I.", "where"),
            };

            return new DialogueGraph("greeting", new[]
            {
                new DialogueNode("greeting", speaker,
                    "Greetings Maggot... Welcome to the first day of the start of your journey. Who are you...",
                    options: ThreeOptions()),
                new DialogueNode("who", speaker,
                    "I suppose you should know. I am the one who shall teach you to fight...",
                    options: ThreeOptions()),
                new DialogueNode("where", speaker,
                    "This is the forest of beginnings. Where you learn to survive.",
                    options: ThreeOptions()),
                new DialogueNode("introduce", speaker,
                    "I am Colonel Powers, your drill sergeant... Get ready maggot, it is time to learn to survive.",
                    nextId: "lack"),
                new DialogueNode("lack", speaker, LackSkillLine),
            });
        }
    }
}
