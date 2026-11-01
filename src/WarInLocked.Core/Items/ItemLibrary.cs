using System.Collections.Generic;

namespace WarInLocked.Core.Items
{
    /// <summary>Known items. More are added here as the game grows.</summary>
    public static class ItemLibrary
    {
        public const string PracticeSwordId = "practice-sword";
        public const string PracticeStaffId = "practice-staff";

        /// <summary>Physical Attack bonus of the sword (#22), added to the base attack of 10 before the Strength modifier.</summary>
        public const int PracticeSwordPhysicalAttack = 5;
        /// <summary>Magical Attack bonus of the staff (#22), added to the base attack of 10 before the Intelligence modifier.</summary>
        public const int PracticeStaffMagicalAttack = 5;

        /// <summary>Dyllan's text from issue #22, verbatim (including his spelling).</summary>
        public const string PracticeSwordDescription =
            "Not overly impressive, the practice sword has a dull edge but for a novice is still use full to learn the craft.";
        public const string PracticeStaffDescription =
            "Looks somewhat like drift wood... But don't let it's looks deceive you, it still dies what it needs to do...";

        /// <summary>The sword Colonel Powers hands out in the tutorial quest (issue #8): Physical Attack +5.</summary>
        public static readonly Item PracticeSword = new Item(PracticeSwordId, "Practice Sword", ItemType.Weapon, EquipSlot.MainHand,
            physicalAttack: PracticeSwordPhysicalAttack, description: PracticeSwordDescription);

        /// <summary>The staff Colonel Powers hands out in tutorial quest 3 (#21, #22): a main-hand weapon with Magical Attack +5.</summary>
        public static readonly Item PracticeStaff = new Item(PracticeStaffId, "Practice Staff", ItemType.Weapon, EquipSlot.MainHand,
            magicalAttack: PracticeStaffMagicalAttack, description: PracticeStaffDescription);

        public static IReadOnlyList<Item> All { get; } = new[] { PracticeSword, PracticeStaff };

        /// <summary>The item with this id, or null.</summary>
        public static Item Get(string id)
        {
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }
    }
}
