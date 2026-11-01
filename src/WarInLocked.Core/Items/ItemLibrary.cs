namespace WarInLocked.Core.Items
{
    /// <summary>Known items. More are added here as the game grows.</summary>
    public static class ItemLibrary
    {
        public const string PracticeSwordId = "practice-sword";

        /// <summary>PLACEHOLDER attack bonus, added to the base attack of 10 before the Strength modifier (see QUESTIONS.md).</summary>
        public const int PracticeSwordBaseDamage = 5;

        /// <summary>The sword Colonel Powers hands out in the tutorial quest (issue #8).</summary>
        public static readonly Item PracticeSword = new Item(PracticeSwordId, "Practice Sword", ItemType.Weapon, EquipSlot.MainHand, baseDamage: PracticeSwordBaseDamage);
    }
}
