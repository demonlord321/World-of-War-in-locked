using System.Linq;
using System;
using WarInLocked.Core.World;
using Xunit;

public class WorldTests
{
    private const string Small = "#####\n#Pgs#\n#gN~#\n#####\n";

    [Fact]
    public void ParsesTilesAndMarkers()
    {
        var map = TileMap.Parse(Small);
        Assert.Equal(5, map.Width);
        Assert.Equal(4, map.Height);
        Assert.Equal((1, 1), map.PlayerStart);
        Assert.Single(map.Npcs);
        Assert.Single(map.SlimeSpawns);
        Assert.Equal(Tile.Water, map[3, 2]);
        Assert.False(map.IsWalkable(0, 0));
        Assert.False(map.IsWalkable(-1, 2));
        Assert.True(map.IsWalkable(2, 1));
    }

    [Fact]
    public void RejectsUnknownSymbolsAndMissingStart()
    {
        Assert.Throws<FormatException>(() => TileMap.Parse("#?#"));
        Assert.Throws<FormatException>(() => TileMap.Parse("###\n#g#\n###"));
    }

    [Fact]
    public void WandererOnlyStepsOntoWalkableTiles()
    {
        var map = TileMap.Parse(Small);
        var slime = new Wanderer(3, 1, new Random(1));
        for (int i = 0; i < 2000; i++)
        {
            slime.Update(0.1f, map);
            Assert.True(map.IsWalkable(slime.TileX, slime.TileY));
        }
    }

    [Fact]
    public void ForestMapLoads()
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../src/WarInLocked.Game/Assets/Maps/forest-of-beginnings.txt");
        var map = TileMap.Parse(System.IO.File.ReadAllText(path));
        Assert.Single(map.Npcs);
        Assert.NotEmpty(map.SlimeSpawns);
    }

    private static TileMap LoadMapFile(string name) =>
        TileMap.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../src/WarInLocked.Game/Assets/Maps/" + name)));

    [Fact]
    public void FenceAndTentBlockWalkingAndDoorsAreWalkableEdgeMarkers()
    {
        var map = TileMap.Parse("#####\n<gFX#\n#P..#\n#####");
        Assert.Equal(Tile.Fence, map[2, 1]);
        Assert.Equal(Tile.Tent, map[3, 1]);
        Assert.False(map.IsWalkable(2, 1));
        Assert.False(map.IsWalkable(3, 1));
        var door = Assert.Single(map.Doors);
        Assert.Equal('<', door.Symbol);
        Assert.True(map.IsWalkable(0, 1));
        Assert.NotNull(map.DoorAt(0, 1));
        Assert.Null(map.DoorAt(1, 1));
    }

    [Fact]
    public void EncampmentMapHasWhiplashNoSlimesAndAWestDoor()
    {
        var camp = LoadMapFile("adventurers-encampment.txt");
        Assert.Equal(2, camp.Npcs.Count);   // whiplash, then barracks-master (reading order)
        Assert.Empty(camp.SlimeSpawns);
        var door = Assert.Single(camp.Doors);
        Assert.Equal('<', door.Symbol);
        Assert.Equal(0, door.X);                 // on the west edge
        Assert.True(camp.IsWalkable(door.X + 1, door.Y));   // the arrival tile just inside
    }

    [Fact]
    public void ForestEastDoorLinesUpWithTheEncampmentWestDoor()
    {
        var forest = LoadMapFile("forest-of-beginnings.txt");
        var door = Assert.Single(forest.Doors);
        Assert.Equal('>', door.Symbol);
        Assert.Equal(forest.Width - 1, door.X);  // on the east edge
        Assert.True(forest.IsWalkable(door.X - 1, door.Y));
    }

    [Fact]
    public void PathMapRunsWestToEastWithSlimesAndNoNpc()
    {
        var path = LoadMapFile("path.txt");
        Assert.Empty(path.Npcs);
        Assert.NotEmpty(path.SlimeSpawns);
        Assert.Equal(2, path.Doors.Count);
        var west = path.Doors.Single(d => d.Symbol == '<');
        var east = path.Doors.Single(d => d.Symbol == '>');
        Assert.Equal(0, west.X);
        Assert.Equal(path.Width - 1, east.X);
        Assert.True(path.IsWalkable(west.X + 1, west.Y));
        Assert.True(path.IsWalkable(east.X - 1, east.Y));
        // The trail is connected from one door to the other.
        var seen = new System.Collections.Generic.HashSet<(int, int)>();
        var todo = new System.Collections.Generic.Stack<(int, int)>();
        todo.Push((west.X, west.Y));
        while (todo.Count > 0)
        {
            var (x, y) = todo.Pop();
            if (!path.IsWalkable(x, y) || !seen.Add((x, y))) continue;
            todo.Push((x + 1, y)); todo.Push((x - 1, y)); todo.Push((x, y + 1)); todo.Push((x, y - 1));
        }
        Assert.Contains((east.X, east.Y), seen);
    }

    [Fact]
    public void BedBlocksWalkingAndSavePointIsWalkableAndListed()
    {
        var map = TileMap.Parse("#####\n#bS.#\n#P..#\n#####");
        Assert.Equal(Tile.Bed, map[1, 1]);
        Assert.False(map.IsWalkable(1, 1));
        Assert.Equal(Tile.SavePoint, map[2, 1]);
        Assert.True(map.IsWalkable(2, 1));
        var sp = Assert.Single(map.SavePoints);
        Assert.Equal((2, 1), (sp.X, sp.Y));
        Assert.True(map.IsSavePoint(2, 1));
        Assert.False(map.IsSavePoint(1, 1));
    }

    [Fact]
    public void EncampmentHasABarracksWithBedsAndMasterClearOfTheOtherThings()
    {
        var camp = LoadMapFile("adventurers-encampment.txt");
        int beds = 0, minX = 99, maxX = 0, minY = 99, maxY = 0;
        for (int y = 0; y < camp.Height; y++)
            for (int x = 0; x < camp.Width; x++)
                if (camp[x, y] == Tile.Bed) { beds++; minX = System.Math.Min(minX, x); maxX = System.Math.Max(maxX, x); minY = System.Math.Min(minY, y); maxY = System.Math.Max(maxY, y); }
        Assert.True(beds >= 16);
        var master = camp.Npcs[1];                 // barracks-master, in front of (north of) the beds
        Assert.True(master.Y < minY);
        Assert.True(camp.IsWalkable(master.X, master.Y + 1));
        // Clear of the ring (x 16..24, y 11..17) and the west door.
        Assert.True(minX > 24);
        Assert.NotEqual(0, minX);
        // Walking from the west door reaches the master's tile and the aisle between the beds.
        var door = camp.Doors.Single();
        var seen = new System.Collections.Generic.HashSet<(int, int)>();
        var todo = new System.Collections.Generic.Stack<(int, int)>();
        todo.Push((door.X, door.Y));
        while (todo.Count > 0)
        {
            var (x, y) = todo.Pop();
            if (!camp.IsWalkable(x, y) || !seen.Add((x, y))) continue;
            todo.Push((x + 1, y)); todo.Push((x - 1, y)); todo.Push((x, y + 1)); todo.Push((x, y - 1));
        }
        Assert.Contains((master.X, master.Y), seen);
        Assert.Contains((32, 16), seen);
        foreach (var sp in camp.SavePoints) Assert.Contains((sp.X, sp.Y), seen);
    }

    [Fact]
    public void SavePointsSitNearColonelPowersInTheForestAndNearWhiplashInTheCamp()
    {
        var forest = LoadMapFile("forest-of-beginnings.txt");
        var colonel = forest.Npcs[0];
        var fsp = Assert.Single(forest.SavePoints);
        Assert.InRange(System.Math.Abs(fsp.X - colonel.X) + System.Math.Abs(fsp.Y - colonel.Y), 1, 4);
        var camp = LoadMapFile("adventurers-encampment.txt");
        var whiplash = camp.Npcs[0];
        var csp = Assert.Single(camp.SavePoints);
        Assert.InRange(System.Math.Abs(csp.X - whiplash.X) + System.Math.Abs(csp.Y - whiplash.Y), 1, 4);
    }
}
