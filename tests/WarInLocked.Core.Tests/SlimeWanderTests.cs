using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

public class SlimeWanderTests
{
    private static string ForestPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "WarInLocked.Game", "Assets", "Maps", "forest-of-beginnings.txt");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("forest-of-beginnings.txt not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void SlimesInTheForestStayLegalAndActuallyMove()
    {
        var map = TileMap.Parse(File.ReadAllText(ForestPath()));
        var rng = new Random(1234);
        var slimes = map.SlimeSpawns.Select(s => new Wanderer(s.X, s.Y, rng)).ToList();
        Assert.NotEmpty(slimes);

        var player = map.PlayerStart;
        var npcs = map.Npcs.Select(n => (n.X, n.Y)).ToList();
        var start = slimes.Select(s => (s.TileX, s.TileY)).ToList();
        var moved = new HashSet<int>();

        for (int step = 0; step < 6000; step++) // 6000 * 0.05s = 5 minutes of play
        {
            foreach (var slime in slimes)
            {
                var self = slime;
                self.Update(0.05f, map, (x, y) =>
                    (x == player.X && y == player.Y) ||
                    npcs.Contains((x, y)) ||
                    slimes.Any(o => o != self && o.TileX == x && o.TileY == y));
            }

            for (int i = 0; i < slimes.Count; i++)
            {
                var s = slimes[i];
                Assert.True(map.IsWalkable(s.TileX, s.TileY), $"slime {i} on a blocked tile at step {step}");
                Assert.False(s.TileX == player.X && s.TileY == player.Y, "slime on the player");
                Assert.DoesNotContain((s.TileX, s.TileY), npcs);
                for (int j = i + 1; j < slimes.Count; j++)
                    Assert.False(s.TileX == slimes[j].TileX && s.TileY == slimes[j].TileY, "two slimes on one tile");
                if ((s.TileX, s.TileY) != start[i]) moved.Add(i);
            }
        }

        Assert.Equal(slimes.Count, moved.Count);
    }

    [Fact]
    public void SameSeedGivesTheSameWandering()
    {
        (int, int)[] Run()
        {
            var map = TileMap.Parse(File.ReadAllText(ForestPath()));
            var rng = new Random(7);
            var slimes = map.SlimeSpawns.Select(s => new Wanderer(s.X, s.Y, rng)).ToList();
            for (int i = 0; i < 1000; i++)
                foreach (var s in slimes) s.Update(0.05f, map);
            return slimes.Select(s => (s.TileX, s.TileY)).ToArray();
        }
        Assert.Equal(Run(), Run());
    }
}
