namespace WarInLocked.Core.Items
{
    /// <summary>The outcome of <c>Character.UseItem</c> (#26).</summary>
    public sealed class ItemUseResult
    {
        public const string NoneLeftText = "None left";
        public const string CannotUseText = "Cannot be used";
        public const string OnCooldownText = "On cooldown";
        public const string FullHealthText = "Already at full health";
        public const string FallenText = "Can't use while fallen";
        public const string InCombatText = "Can't use in combat";
        public const string FullyRecoveredText = "Already fully recovered";

        private ItemUseResult(bool success, string reason, Item item, int healed, int energy = 0)
        {
            Success = success;
            Reason = reason;
            Item = item;
            Healed = healed;
            EnergyRestored = energy;
        }

        public static ItemUseResult Used(Item item, int healed, int energy = 0) => new ItemUseResult(true, null, item, healed, energy);
        public static ItemUseResult Refused(Item item, string reason) => new ItemUseResult(false, reason, item, 0);

        public bool Success { get; }
        /// <summary>Why it did not work ("On cooldown", "Already at full health", "None left", "Cannot be used", "Can't use in combat", "Already fully recovered"), or null on success. Nothing is spent when refused.</summary>
        public string Reason { get; }
        public Item Item { get; }
        /// <summary>Health actually restored (less than the heal amount when it was capped by max health).</summary>
        public int Healed { get; }
        /// <summary>Energy actually restored (rations), 0 for other items.</summary>
        public int EnergyRestored { get; }
    }
}
