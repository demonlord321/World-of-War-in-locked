using System.Collections.Generic;

namespace WarInLocked.Core.Items
{
    /// <summary>Known items. More are added here as the game grows.</summary>
    public static class ItemLibrary
    {
        public const string PracticeSwordId = "practice-sword";
        public const string PracticeStaffId = "practice-staff";
        public const string LesserHealingPotionId = "potion-lesser-healing";
        public const string AdventurersRationsId = "adventurers-rations";

        /// <summary>Health the Potion of lesser healing restores (#26).</summary>
        public const int LesserHealingPotionHeal = 50;
        /// <summary>Shared cooldown (seconds) after using any consumable (#26).</summary>
        public const double ConsumableCooldownSeconds = 5.0;
        /// <summary>PLACEHOLDER stack size for consumables (#26).</summary>
        public const int ConsumableStackSize = 10;

        /// <summary>Dyllan's text from issue #26, verbatim.</summary>
        public const string LesserHealingPotionDescription = "A dull red colour. Tastes oddly like strawberries.";
        /// <summary>PLACEHOLDER (Dyllan gave no text for the rations).</summary>
        public const string AdventurersRationsDescription = "Dried meat and hard bread. Enough to keep an adventurer going.";

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

        /// <summary>Potion of lesser healing (#26): heals 50 HP, shared 5 s cooldown, stacks to 10.</summary>
        public static readonly Item LesserHealingPotion = new Item(LesserHealingPotionId, "Potion of lesser healing", ItemType.Consumable,
            maxStack: ConsumableStackSize, description: LesserHealingPotionDescription,
            healAmount: LesserHealingPotionHeal, useCooldownSeconds: ConsumableCooldownSeconds);

        /// <summary>Adventurer's rations (#25): PLACEHOLDER item with no effect yet.</summary>
        public static readonly Item AdventurersRations = new Item(AdventurersRationsId, "Adventurer's rations", ItemType.Misc,
            maxStack: ConsumableStackSize, description: AdventurersRationsDescription);

        public static IReadOnlyList<Item> All { get; } = new[] { PracticeSword, PracticeStaff, LesserHealingPotion, AdventurersRations };

        /// <summary>The item with this id, or null.</summary>
        public static Item Get(string id)
        {
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }
    }
}
