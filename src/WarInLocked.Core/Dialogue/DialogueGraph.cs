using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Dialogue
{
    /// <summary>A choice the player can pick. <see cref="NextId"/> is the node it leads to; null or empty ends the dialogue.</summary>
    public sealed class DialogueOption
    {
        public DialogueOption(string text, string nextId)
        {
            Text = text;
            NextId = nextId;
        }

        /// <summary>What the player says. May contain {name}.</summary>
        public string Text { get; }
        public string NextId { get; }
    }

    /// <summary>
    /// One screen of dialogue: who speaks, what they say, and what happens next.
    /// With <see cref="Options"/> the player picks one; without them the player continues to
    /// <see cref="NextId"/>, or the dialogue ends if that is null or empty.
    /// </summary>
    public sealed class DialogueNode
    {
        public DialogueNode(string id, string speaker, string text, string nextId = null, IEnumerable<DialogueOption> options = null)
        {
            Id = id;
            Speaker = speaker;
            Text = text;
            NextId = nextId;
            var list = new List<DialogueOption>();
            if (options != null) list.AddRange(options);
            Options = list;
        }

        public string Id { get; }
        public string Speaker { get; }
        /// <summary>What is said. May contain {name}.</summary>
        public string Text { get; }
        public string NextId { get; }
        public IReadOnlyList<DialogueOption> Options { get; }
    }

    /// <summary>A set of nodes that link to each other, including loop-backs.</summary>
    public sealed class DialogueGraph
    {
        private readonly Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>();

        public DialogueGraph(string startId, IEnumerable<DialogueNode> nodes)
        {
            foreach (DialogueNode node in nodes)
            {
                if (_nodes.ContainsKey(node.Id)) throw new ArgumentException("Duplicate dialogue node id: " + node.Id);
                _nodes.Add(node.Id, node);
            }
            StartId = startId;
            if (!_nodes.ContainsKey(startId)) throw new ArgumentException("Start node not found: " + startId);

            // Catch typos in links early.
            foreach (DialogueNode node in _nodes.Values)
            {
                CheckLink(node, node.NextId);
                foreach (DialogueOption option in node.Options) CheckLink(node, option.NextId);
            }
        }

        public string StartId { get; }

        public DialogueNode this[string id]
        {
            get
            {
                if (!_nodes.TryGetValue(id, out DialogueNode node)) throw new KeyNotFoundException("No dialogue node: " + id);
                return node;
            }
        }

        public IEnumerable<DialogueNode> Nodes => _nodes.Values;

        private void CheckLink(DialogueNode from, string toId)
        {
            if (!string.IsNullOrEmpty(toId) && !_nodes.ContainsKey(toId))
                throw new ArgumentException("Dialogue node '" + from.Id + "' links to missing node '" + toId + "'.");
        }
    }
}
