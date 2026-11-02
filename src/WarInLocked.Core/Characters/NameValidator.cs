namespace WarInLocked.Core.Characters
{
    public enum NameError
    {
        None,
        Empty,
        TooLong,
        InvalidCharacter,
    }

    /// <summary>
    /// Rules for a character name (#45). Dyllan: "No level cap and for now no restriction on names. No stat cap either." So the only rule left is that
    /// the name is not empty (a nameless character cannot be saved); any length and any characters are accepted, apart from control characters.
    /// </summary>
    public static class NameValidator
    {
        /// <summary>NOT a rule any more. A suggested limit for text boxes so a long name still fits the screen; <see cref="Validate"/> does not enforce it.</summary>
        public const int MaxLength = 20;

        /// <summary>Removes leading and trailing whitespace. Null becomes an empty string.</summary>
        public static string Normalize(string name)
        {
            return name == null ? string.Empty : name.Trim();
        }

        /// <summary>True for characters that may appear in a name (used to filter typing too).</summary>
        public static bool IsAllowedCharacter(char c)
        {
            return !char.IsControl(c);
        }

        public static NameError Validate(string name)
        {
            string trimmed = Normalize(name);
            if (trimmed.Length == 0) return NameError.Empty;
            foreach (char c in trimmed)
            {
                if (!IsAllowedCharacter(c)) return NameError.InvalidCharacter;   // control characters only
            }
            return NameError.None;
        }

        public static bool IsValid(string name)
        {
            return Validate(name) == NameError.None;
        }
    }
}
