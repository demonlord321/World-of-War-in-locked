using System;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Interaction
{
    /// <summary>
    /// The Barracks Master in the Adventurers encampment (#55). Flow: <see cref="CreateDialogue"/> shows his welcome and the menu; when the dialogue has ended,
    /// pass <c>runner.Action</c> to <see cref="Apply"/> (it needs the home bind point for "Set as home") and show the notice it gives.
    /// </summary>
    public static class BarracksMaster
    {
        /// <summary>His NPC id in the map data (map <see cref="MapIds.AdventurersEncampment"/>).</summary>
        public const string NpcId = DialogueLibrary.BarracksMasterId;

        /// <summary>Rest: health and energy back to full and the game clock moves on by this many hours (Dyllan: 3).</summary>
        public const double RestHours = 3.0;

        public const string DeadNotice = "You cannot rest while fallen";   // PLACEHOLDER
        public const string RestNotice = "You rest. Health and energy restored";   // PLACEHOLDER
        public const string HomeNotice = "Home set: this is where you wake after a fall";   // PLACEHOLDER
        public const string SleepNoticePrefix = "You sleep until morning";   // PLACEHOLDER; the time is appended

        /// <summary>His dialogue now: the menu has "Sleep until morning" only at night on the character's clock.</summary>
        public static DialogueRunner CreateDialogue(Character character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            return new DialogueRunner(DialogueLibrary.BarracksMasterDialogue(character.Clock.IsNight), character);
        }

        /// <summary>
        /// The bind point for "Set as home": a free walkable tile next to the Barracks Master (south first, then east, west, north, diagonals; beds block).
        /// The game passes the encampment map and his tile.
        /// </summary>
        public static BindPoint HomePointFor(TileMap encampmentMap, int masterX, int masterY) =>
            BindPoint.NextToNpc(MapIds.AdventurersEncampment, encampmentMap, masterX, masterY);

        /// <summary>
        /// Does what the picked option says. <paramref name="action"/> is <c>DialogueRunner.Action</c> (null or unknown does nothing and returns false).
        /// <list type="bullet">
        /// <item><see cref="BarracksActions.Rest"/>: full health and energy, clock +3 hours.</item>
        /// <item><see cref="BarracksActions.Sleep"/>: full health and energy and the clock jumps to 8:00 am (the next morning; the same day if it is already after midnight). Only at night, otherwise refused.</item>
        /// <item><see cref="BarracksActions.SetAsHome"/>: <see cref="Character.SetBindPoint"/> with <paramref name="homePoint"/>.</item>
        /// </list>
        /// <paramref name="notice"/> is the short message to show.
        /// </summary>
        public static bool Apply(Character character, string action, BindPoint homePoint, out string notice)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            notice = null;
            switch (action)
            {
                case BarracksActions.Rest:
                    if (character.IsDead) { notice = DeadNotice; return false; }
                    character.RecoverFully();
                    character.Clock.Advance(RestHours);
                    notice = RestNotice;
                    return true;
                case BarracksActions.Sleep:
                    if (character.IsDead) { notice = DeadNotice; return false; }
                    if (!character.Clock.IsNight) return false;   // the option only exists at night
                    character.RecoverFully();
                    character.Clock.AdvanceToMorning();
                    notice = SleepNoticePrefix + ": " + character.Clock.Text;
                    return true;
                case BarracksActions.SetAsHome:
                    if (homePoint == null) throw new ArgumentNullException(nameof(homePoint));
                    character.SetBindPoint(homePoint);
                    notice = HomeNotice;
                    return true;
                default:
                    return false;
            }
        }
    }
}
