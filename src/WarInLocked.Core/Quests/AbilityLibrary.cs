namespace WarInLocked.Core.Quests
{
    /// <summary>Ability ids and their display names.</summary>
    public static class AbilityLibrary
    {
        public const string Slash = "slash";

        public static string NameOf(string id)
        {
            switch (id)
            {
                case Slash: return "Slash";
                default: return id;
            }
        }
    }
}
