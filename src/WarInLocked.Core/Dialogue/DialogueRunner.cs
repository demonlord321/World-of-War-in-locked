using System;
using System.Collections.Generic;
using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Dialogue
{
    /// <summary>Plays a <see cref="DialogueGraph"/>: the UI reads the current line and options, and calls Choose or Advance.</summary>
    public sealed class DialogueRunner
    {
        private readonly DialogueGraph _graph;
        private readonly IDictionary<string, string> _variables;
        private DialogueNode _current;

        /// <param name="variables">Values for {placeholders}, e.g. "name". See <see cref="VariablesFor"/>.</param>
        public DialogueRunner(DialogueGraph graph, IDictionary<string, string> variables)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _variables = variables ?? new Dictionary<string, string>();
            _current = graph[graph.StartId];
        }

        public DialogueRunner(DialogueGraph graph, Character character) : this(graph, VariablesFor(character)) { }

        /// <summary>The placeholders a Character provides: {name}, {race} and {class}.</summary>
        public static Dictionary<string, string> VariablesFor(Character character)
        {
            var vars = new Dictionary<string, string>();
            if (character == null) return vars;
            vars["name"] = character.Name;
            vars["race"] = character.Race.ToString();
            vars["class"] = character.Distribution.ToString();
            return vars;
        }

        /// <summary>Replaces each {key} with its value. Unknown placeholders are left as they are.</summary>
        public static string Substitute(string text, IDictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(text) || variables == null) return text;
            foreach (KeyValuePair<string, string> pair in variables)
                text = text.Replace("{" + pair.Key + "}", pair.Value);
            return text;
        }

        /// <summary>True once the dialogue is over; the other members must not be used after that.</summary>
        public bool IsEnded { get; private set; }

        public string Speaker => Current.Speaker;

        /// <summary>The line being said, with placeholders filled in.</summary>
        public string Text => Substitute(Current.Text, _variables);

        public bool HasOptions => Current.Options.Count > 0;

        /// <summary>The player's choices, with placeholders filled in. Empty when the player just continues.</summary>
        public IReadOnlyList<string> Options
        {
            get
            {
                var list = new List<string>();
                foreach (DialogueOption option in Current.Options) list.Add(Substitute(option.Text, _variables));
                return list;
            }
        }

        /// <summary>The action of the last option the player picked (e.g. open the shop, #28), or null. Read it once <see cref="IsEnded"/>.</summary>
        public string Action { get; private set; }

        /// <summary>Id of the current node (handy for tests and debugging).</summary>
        public string CurrentNodeId => Current.Id;

        private DialogueNode Current
        {
            get
            {
                if (IsEnded) throw new InvalidOperationException("The dialogue has ended.");
                return _current;
            }
        }

        /// <summary>Picks option <paramref name="index"/> (0-based).</summary>
        public void Choose(int index)
        {
            DialogueNode node = Current;
            if (node.Options.Count == 0) throw new InvalidOperationException("This line has no options; call Advance.");
            if (index < 0 || index >= node.Options.Count) throw new ArgumentOutOfRangeException(nameof(index));
            Action = node.Options[index].Action;
            GoTo(node.Options[index].NextId);
        }

        /// <summary>Continues past a line that has no options.</summary>
        public void Advance()
        {
            DialogueNode node = Current;
            if (node.Options.Count > 0) throw new InvalidOperationException("This line has options; call Choose.");
            GoTo(node.NextId);
        }

        private void GoTo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                IsEnded = true;
                return;
            }
            _current = _graph[id];
        }
    }
}
