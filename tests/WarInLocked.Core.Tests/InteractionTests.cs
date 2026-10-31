using WarInLocked.Core.Interaction;
using WarInLocked.Core.Story;

namespace WarInLocked.Core.Tests;

public class InteractionTests
{
    [Theory]
    [InlineData(5, 5, 5, 4, true)]   // above
    [InlineData(5, 5, 5, 6, true)]   // below
    [InlineData(5, 5, 4, 5, true)]   // left
    [InlineData(5, 5, 6, 5, true)]   // right
    [InlineData(5, 5, 6, 6, true)]   // diagonal
    [InlineData(5, 5, 5, 5, false)]  // same tile
    [InlineData(5, 5, 7, 5, false)]  // two away
    [InlineData(5, 5, 5, 8, false)]
    [InlineData(5, 5, 7, 7, false)]
    public void AdjacencyIsOneTileIncludingDiagonals(int px, int py, int nx, int ny, bool expected)
    {
        Assert.Equal(expected, TalkTarget.IsAdjacent(px, py, nx, ny));
    }

    [Fact]
    public void FindReturnsNullWhenNobodyIsNear()
    {
        var npcs = new[] { new NpcSpot("colonel-powers", "colonel-powers", 22, 11) };
        Assert.Null(TalkTarget.Find(20, 13, npcs));
        Assert.Null(TalkTarget.Find(0, 0, new NpcSpot[0]));
    }

    [Fact]
    public void FindReturnsTheNpcNextToThePlayer()
    {
        var colonel = new NpcSpot("colonel-powers", "colonel-powers", 22, 11);
        Assert.Same(colonel, TalkTarget.Find(22, 12, new[] { colonel }));
        Assert.Same(colonel, TalkTarget.Find(21, 12, new[] { colonel }));
    }

    [Fact]
    public void FindPrefersAStraightNeighbourOverADiagonalOne()
    {
        var diagonal = new NpcSpot("a", "a", 4, 4);
        var straight = new NpcSpot("b", "b", 5, 4);
        Assert.Same(straight, TalkTarget.Find(5, 5, new[] { diagonal, straight }));
    }

    [Fact]
    public void OpeningStoryUsesDyllansWordsWithTyposFixed()
    {
        Assert.StartsWith("A world locked in endless conflict, with nations all fighting for the right to rule this world.", StoryTexts.Opening);
        Assert.Contains("Countless factions all fighting for control.", StoryTexts.Opening);
        Assert.Contains("Choose your path, join a faction, fight to be your factions champion.", StoryTexts.Opening);
        Assert.EndsWith("Welcome to the World of War-in-locked.", StoryTexts.Opening);
        Assert.DoesNotContain("controle", StoryTexts.Opening);
        Assert.DoesNotContain("Chose ", StoryTexts.Opening);
    }
}
