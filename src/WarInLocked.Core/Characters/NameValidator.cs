namespace WarInLocked.Core.Characters
{
    public enum NameError
    {
        None,
        Empty,
        TooLong,
        InvalidCharacter,
    }

    /// <summary>Rules for a character name: 1 to 20 characters of letters, digits, spaces, ' or -.</summary>
    public static class NameValidator
    {
        public const int MaxLength = 20;

        /// <summary>Removes leading and trailing whitespace. Null becomes an empty string.</summary>
        public static string Normalize(string name)
        {
            return name == null ? string.Empty : name.Trim();
        }

        /// <summary>True for characters that may appear in a name (used to filter typing too).</summary>
        public static bool IsAllowedCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == ' ' || c == '\'' || c == '-';
        }

        public static NameError Validate(string name)
        {
            string trimmed = Normalize(name);
            if (trimmed.Length == 0) return NameError.Empty;
            if (trimmed.Length > MaxLength) return NameError.TooLong;
            foreach (char c in trimmed)
            {
                if (!IsAllowedCharacter(c)) return NameError.InvalidCharacter;
            }
            return NameError.None;
        }

        public static bool IsValid(string name)
        {
            return Validate(name) == NameError.None;
        }
    }
}
