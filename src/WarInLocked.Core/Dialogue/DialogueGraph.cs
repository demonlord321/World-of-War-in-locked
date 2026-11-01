using System;
using System.Collections.Generic;

namespace WarInLocked.Core.Dialogue
{
    /// <summary>A choice the player can pick. <see cref="NextId"/> is the node it leads to; null or empty ends the dialogue.</summary>
    public sealed class DialogueOption
    {
        public DialogueOption(string text, string nextId, string action = null)
        {
            Text = text;
            NextId = nextId;
            Action = action;
        }

        /// <summary>
        /// Optional signal for the game when this option is picked (e.g. <see cref="DialogueActions.OpenShop"/>), or null.
        /// The runner exposes it as <see cref="DialogueRunner.Action"/>.
        /// </summary>
        public string Action { get; }

        /// <summary>What the player says. May contain {name}.</summary>
        public string Text { get; }
        public string NextId { get; }
    }

    /// <summary>The actions of the Barracks Master's menu (#55); see <c>BarracksMaster.Apply</c>.</summary>
    public static class BarracksActions
    {
        public const string Rest = "barracks:rest";
        public const string Sleep = "barracks:sleep";
        public const string SetAsHome = "barracks:home";
    }

    /// <summary>Signals a dialogue option can give to the game (#28).</summary>
    public static class DialogueActions
    {
        public const string ShopPrefix = "shop:";
        /// <summary>The action for "open the shop with this id" (see <c>ShopLibrary.Get</c>).</summary>
        public static string OpenShop(string shopId) => ShopPrefix + shopId;
        public static bool IsOpenShop(string action) => action != null && action.StartsWith(ShopPrefix);
        /// <summary>The shop id inside an OpenShop action, or null.</summary>
        public static string ShopIdOf(string action) => IsOpenShop(action) ? action.Substring(ShopPrefix.Length) : null;
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
