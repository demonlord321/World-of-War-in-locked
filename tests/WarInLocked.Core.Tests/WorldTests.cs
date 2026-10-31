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
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../src/WarInLocked.Game/Assets/Maps/forest-of-beginning.txt");
        var map = TileMap.Parse(System.IO.File.ReadAllText(path));
        Assert.Single(map.Npcs);
        Assert.NotEmpty(map.SlimeSpawns);
    }
}
