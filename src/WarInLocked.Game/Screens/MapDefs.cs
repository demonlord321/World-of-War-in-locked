using WarInLocked.Core.Combat;
using WarInLocked.Core.World;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #53: which maps exist, their files, NPC ids (in the order of the N markers in the map text) and where each edge door leads.
/// A door symbol is the edge it sits on: '&lt;' west, '&gt;' east, '^' north, 'v' south. Leaving through one edge arrives just inside
/// the opposite-edge door of the destination map, so the doors always line up directionally.
/// </summary>
public static class MapDefs
{
    public const string ForestId = BindPoint.ForestOfBeginningsMapId;
    public const string PathId = "path";
    public const string EncampmentId = "adventurers-encampment";

    /// <param name="SlimeLevels">Level range the map's slimes are drawn from, uniformly (forest 1-3, Path 4-5).</param>
    public sealed record Def(string Id, string File, string Title, string[] NpcIds, (int Min, int Max) SlimeLevels = default);

    private static readonly Dictionary<string, Def> Maps = new()
    {
        [ForestId] = new(ForestId, "forest-of-beginnings.txt", "The Forest of Beginnings", new[] { "colonel-powers" }, (Enemy.MinSlimeLevel, Enemy.MaxSlimeLevel)),
        [PathId] = new(PathId, "path.txt", "The Path", new string[0], (Enemy.PathMinSlimeLevel, Enemy.PathMaxSlimeLevel)),
        [EncampmentId] = new(EncampmentId, "adventurers-encampment.txt", "Adventurers' Encampment", new[] { "whiplash", "barracks-master" }),
    };

    // (map, door symbol) -> destination map. Route west to east: Forest -> Path -> Encampment (each edge leads to the opposite edge of the next map).
    private static readonly Dictionary<(string, char), string> Doors = new()
    {
        [(ForestId, '>')] = PathId,
        [(PathId, '<')] = ForestId,
        [(PathId, '>')] = EncampmentId,
        [(EncampmentId, '<')] = PathId,
    };

    private static readonly Dictionary<string, string> NpcNames = new()
    {
        ["colonel-powers"] = "Colonel Powers",
        ["whiplash"] = "Whiplash",
        ["barracks-master"] = "Barracks Master",
    };

    public static bool Exists(string mapId) => Maps.ContainsKey(mapId);
    public static Def Get(string mapId) => Maps[mapId];
    public static string? Destination(string mapId, char door) => Doors.TryGetValue((mapId, door), out var d) ? d : null;
    /// <summary>NPCs with a dialogue to talk to. The Barracks Master is only placed and labelled for now (no Core dialogue yet).</summary>
    public static bool HasDialogue(string npcId) => npcId != "barracks-master";
    public static string NpcName(string npcId) => NpcNames.TryGetValue(npcId, out var n) ? n : npcId;

    public static char Opposite(char door) => door switch { '<' => '>', '>' => '<', '^' => 'v', _ => '^' };

    /// <summary>The walkable tile just inside the given door of a map (where a player arrives), or null if the map has no such door.</summary>
    public static (int X, int Y)? ArrivalTile(TileMap map, char door)
    {
        foreach (var d in map.Doors)
            if (d.Symbol == door)
                return door switch { '<' => (d.X + 1, d.Y), '>' => (d.X - 1, d.Y), '^' => (d.X, d.Y + 1), _ => (d.X, d.Y - 1) };
        return null;
    }
}
