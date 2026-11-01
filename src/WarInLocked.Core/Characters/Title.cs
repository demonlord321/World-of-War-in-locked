using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Characters
{
    /// <summary>
    /// A title the character can earn and wear (#27). One title is equipped at a time. While equipped it adds
    /// up to <see cref="AllStatsBonus"/> to every stat, never taking a stat up to or past its next milestone (#33, see <see cref="Character.TitleBonus"/> and <see cref="Character.EffectiveStats"/>).
    /// </summary>
    public sealed class Title
    {
        public Title(string id, string name, string description, int allStatsBonus)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A title needs an id.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A title needs a name.", nameof(name));
            Id = id;
            Name = name;
            Description = description ?? "";
            AllStatsBonus = allStatsBonus;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        /// <summary>Added to every one of the eight stats while the title is equipped.</summary>
        public int AllStatsBonus { get; }

        /// <summary>The bonus ready to print, e.g. "+2 to all stats when equipped." Empty if there is none.</summary>
        public IReadOnlyList<string> BonusLines
        {
            get
            {
                var lines = new List<string>();
                if (AllStatsBonus != 0) lines.Add((AllStatsBonus > 0 ? "+" : "") + AllStatsBonus + " to all stats when equipped.");
                return lines;
            }
        }

        public override string ToString() => Name;
    }

    /// <summary>Known titles.</summary>
    public static class TitleLibrary
    {
        public const string AdventurerId = "adventurer";

        /// <summary>Dyllan's text from issue #27, verbatim (the full description adds <see cref="AdventurerBonusExplanation"/>).</summary>
        public const string AdventurerDescription =
            "You have completed the tutorial. The Adventurer is one who is prepared for the upcoming adventure.";

        /// <summary>#33 (Dyllan): the title's bonus stops short of the next milestone; it is part of the title's description.</summary>
        public const string AdventurerBonusExplanation =
            "Its +2 never takes a stat up to or past its next milestone (10, 25, 50): a stat of 8 gets +1 (9), a stat of 9 gets +0, and a stat of 10 gets the full +2 (12). "
            + "Invest your own points to reach the milestone and the +2 applies again.";

        /// <summary>Reward of quest 4 "Shopping before adventure": +2 to all stats when equipped.</summary>
        public static readonly Title Adventurer = new Title(AdventurerId, "Adventurer", AdventurerDescription + " " + AdventurerBonusExplanation, 2);

        public static IReadOnlyList<Title> All { get; } = new[] { Adventurer };

        /// <summary>The title with this id, or null.</summary>
        public static Title Get(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }
    }
}
