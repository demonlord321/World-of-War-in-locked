namespace WarInLocked.Core.Items
{
    /// <summary>The outcome of <c>Character.UseItem</c> (#26).</summary>
    public sealed class ItemUseResult
    {
        public const string NoneLeftText = "None left";
        public const string CannotUseText = "Cannot be used";
        public const string OnCooldownText = "On cooldown";
        public const string FullHealthText = "Already at full health";

        private ItemUseResult(bool success, string reason, Item item, int healed)
        {
            Success = success;
            Reason = reason;
            Item = item;
            Healed = healed;
        }

        public static ItemUseResult Used(Item item, int healed) => new ItemUseResult(true, null, item, healed);
        public static ItemUseResult Refused(Item item, string reason) => new ItemUseResult(false, reason, item, 0);

        public bool Success { get; }
        /// <summary>Why it did not work ("On cooldown", "Already at full health", "None left", "Cannot be used"), or null on success. Nothing is spent when refused.</summary>
        public string Reason { get; }
        public Item Item { get; }
        /// <summary>Health actually restored (less than the heal amount when it was capped by max health).</summary>
        public int Healed { get; }
    }
}
