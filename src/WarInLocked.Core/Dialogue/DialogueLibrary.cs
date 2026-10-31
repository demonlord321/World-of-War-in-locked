using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Dialogue
{
    /// <summary>All dialogues, found by id. NPCs refer to these ids in the map file.</summary>
    public static class DialogueLibrary
    {
        public const string ColonelPowersId = "colonel-powers";

        public static DialogueGraph Get(string id)
        {
            if (id == ColonelPowersId) return ColonelPowers();
            throw new KeyNotFoundException("No dialogue with id '" + id + "'.");
        }

        public static bool Exists(string id) => id == ColonelPowersId;

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
                    "I am Colonel Powers, your drill sergeant... Get ready maggot, it is time to learn to survive."),
            });
        }
    }
}
