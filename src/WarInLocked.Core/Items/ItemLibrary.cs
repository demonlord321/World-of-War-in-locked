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
        public const string SlimeJellyId = "slime-jelly";
        /// <summary>PLACEHOLDER (#36): what a shop pays for one Slime Jelly, in copper. Dyllan gave no price.</summary>
        public const int SlimeJellySellPrice = 3;
        /// <summary>PLACEHOLDER stack size of Slime Jelly (#36).</summary>
        public const int SlimeJellyStackSize = 20;
        /// <summary>PLACEHOLDER flavour line for Slime Jelly (#36): Dyllan only said it has no use but selling.</summary>
        public const string SlimeJellyDescription = "A wobbling blob of jelly. Good for nothing but selling."; 

        /// <summary>Health the Potion of lesser healing restores (#26).</summary>
        public const int LesserHealingPotionHeal = 50;
        /// <summary>Shared cooldown (seconds) after using any consumable (#26).</summary>
        public const double ConsumableCooldownSeconds = 5.0;
        /// <summary>PLACEHOLDER stack size for consumables (#26).</summary>
        public const int ConsumableStackSize = 10;

        /// <summary>Dyllan's text from issue #26, verbatim.</summary>
        public const string LesserHealingPotionDescription = "A dull red colour. Tastes oddly like strawberries.";
        /// <summary>Dyllan's text from issue #26, verbatim. It describes the effect, so it is the description (there is no flavour line).</summary>
        public const string AdventurersRationsDescription =
            "When outside of combat, recover completely. Up till level 10. After that the rations are only half as effective.";

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

        /// <summary>
        /// Adventurer's rations (#25, #26): out of combat only; restore health and energy completely up to level 10, half of the max above that.
        /// Shares the item cooldown.
        /// </summary>
        public static readonly Item AdventurersRations = new Item(AdventurersRationsId, "Adventurer's rations", ItemType.Consumable,
            maxStack: ConsumableStackSize, description: AdventurersRationsDescription,
            useCooldownSeconds: ConsumableCooldownSeconds, recoversVitals: true);

        /// <summary>Slime Jelly (#36): every slime drops exactly one when it dies, whatever its level. No use except selling (a Misc item, not usable or equippable); stacks.</summary>
        public static readonly Item SlimeJelly = new Item(SlimeJellyId, "Slime Jelly", ItemType.Misc, maxStack: SlimeJellyStackSize,
            description: SlimeJellyDescription, sellPriceCopper: SlimeJellySellPrice);

        public static IReadOnlyList<Item> All { get; } = new[] { PracticeSword, PracticeStaff, LesserHealingPotion, AdventurersRations, SlimeJelly };

        /// <summary>The item with this id, or null.</summary>
        public static Item Get(string id)
        {
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }
    }
}
