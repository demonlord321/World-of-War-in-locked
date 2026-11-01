using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Core.World;

namespace WarInLocked.Core.Tests;

/// <summary>#29 Respawn at the bind point.</summary>
public class RespawnTests
{
    private static Character Hero()
    {
        var c = new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = StatDistribution.Warrior }.Create();
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    private static void Fall(Character c) => c.TakeDamage(c.MaxHealth + 10);

    private static TileMap ForestMap()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "src", "WarInLocked.Game", "Assets", "Maps", "forest-of-beginnings.txt")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return TileMap.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "src", "WarInLocked.Game", "Assets", "Maps", "forest-of-beginnings.txt")));
    }

    [Fact]
    public void FallenMessageIsDyllansText()
    {
        Assert.Equal("You have fallen... The goddess has seen fit to grant you another chance.", Character.FallenMessage);
        Assert.Equal("You have fallen...", Character.FallenTitle);
        Assert.Equal("The goddess has seen fit to grant you another chance.", Character.FallenSubtitle);
        Assert.Equal(5.0, Character.RespawnDelaySeconds);
    }

    [Fact]
    public void DefaultBindPointIsNextToColonelPowersOnTheForestMap()
    {
        var map = ForestMap();
        var colonel = map.Npcs[0];
        var expected = BindPoint.NextToNpc(BindPoint.ForestOfBeginningsMapId, map, colonel.X, colonel.Y);
        Assert.Equal(expected, Hero().BindPoint);
        Assert.Equal(BindPoint.Tutorial, Hero().BindPoint);
        Assert.True(map.IsWalkable(expected.TileX, expected.TileY));
        Assert.True(System.Math.Abs(expected.TileX - colonel.X) <= 1 && System.Math.Abs(expected.TileY - colonel.Y) <= 1);
        Assert.False(expected.TileX == colonel.X && expected.TileY == colonel.Y);
    }

    [Fact]
    public void SettingABindPointReplacesTheOldOne()
    {
        var c = Hero();
        var inn = new BindPoint("harbour-town", 4, 7);
        c.SetBindPoint(inn);
        Assert.Equal(inn, c.BindPoint);
        var other = new BindPoint("hill-town", 1, 1);
        c.SetBindPoint(other);
        Assert.Equal(other, c.BindPoint);
        Assert.NotEqual(inn, c.BindPoint);
        Assert.Throws<ArgumentNullException>(() => c.SetBindPoint(null!));
    }

    [Fact]
    public void FallingStartsTheCountdownAndRespawnsAfterFiveSeconds()
    {
        var c = Hero();
        Assert.Equal(0, c.RespawnRemaining);
        var spots = new List<BindPoint>();
        c.Respawned += p => spots.Add(p);
        Fall(c);
        Assert.True(c.IsDead);
        Assert.Equal(5.0, c.RespawnRemaining);
        c.Combat.Update(4.9);
        Assert.True(c.IsDead);
        Assert.Equal(0.1, c.RespawnRemaining, 3);
        Assert.Equal(0.98, c.RespawnProgress, 3);
        Assert.Empty(spots);
        c.Combat.Update(0.2);
        Assert.False(c.IsDead);
        Assert.Single(spots);
        Assert.Equal(c.BindPoint, spots[0]);
        Assert.Equal(0, c.RespawnRemaining);
    }

    [Fact]
    public void RespawnUsesTheLatestBindPointAndDirectUpdateWorksToo()
    {
        var c = Hero();
        var inn = new BindPoint("harbour-town", 4, 7);
        c.SetBindPoint(inn);
        BindPoint seen = null!;
        c.Respawned += p => seen = p;
        Fall(c);
        c.UpdateRespawn(5);
        Assert.Equal(inn, seen);
    }

    [Fact]
    public void RespawnRestoresFullHealthAndEnergyAndClearsCombat()
    {
        var c = Hero();
        c.SpendEnergy(20);
        Fall(c);
        Assert.True(c.InCombat);
        c.UpdateRespawn(5);
        Assert.Equal(c.MaxHealth, c.Health);
        Assert.Equal(c.MaxEnergy, c.Energy);
        Assert.False(c.InCombat);
    }

    [Fact]
    public void RespawnKeepsXpCopperItemsAndQuests()
    {
        var c = Hero();
        c.GainXp(30);
        c.GainCopper(40);
        c.Inventory.Add(ItemLibrary.LesserHealingPotion, 2);
        int xp = c.Xp; int copper = c.Copper;
        Fall(c);
        c.UpdateRespawn(5);
        Assert.Equal(xp, c.Xp);
        Assert.Equal(copper, c.Copper);
        Assert.Equal(2, c.Inventory.CountOf(ItemLibrary.LesserHealingPotionId));
    }

    [Fact]
    public void RespawnCalmsEnemiesThroughTheHelper()
    {
        var c = Hero();
        var slimes = new List<Enemy> { Enemy.NewSlime(1), Enemy.NewSlime(3) };
        foreach (var s in slimes) s.TileX = s.TileY = 5;
        slimes[0].TakeDamage(1); slimes[1].TakeDamage(1);
        Assert.All(slimes, s => Assert.True(s.IsAggro));
        c.Respawned += _ => CombatController.CalmEnemies(slimes);
        Fall(c);
        c.UpdateRespawn(5);
        Assert.All(slimes, s => { Assert.False(s.IsAggro); Assert.Equal(0, s.AttackCooldownRemaining); });
        Assert.Equal(0, c.Combat.UpdateEnemies(slimes, 60, 5, 5));
        slimes[0].TakeDamage(1);   // can be provoked again
        Assert.True(slimes[0].IsAggro);
    }

    [Fact]
    public void ADeadCharacterCannotUseItemsAndFallsOnlyOnce()
    {
        var c = Hero();
        c.Inventory.Add(ItemLibrary.LesserHealingPotion);
        int died = 0;
        c.Died += () => died++;
        Fall(c);
        Assert.Equal("Can't use while fallen", c.CanUse(ItemLibrary.LesserHealingPotionId));
        Assert.False(c.UseItem(ItemLibrary.LesserHealingPotionId).Success);
        c.TakeDamage(5);
        Assert.Equal(5.0, c.RespawnRemaining);   // more damage does not restart the countdown
        Assert.Equal(1, died);
        c.UpdateRespawn(5);
        Fall(c);
        Assert.Equal(2, died);                    // can fall again after respawning
    }

    [Fact]
    public void UpdateRespawnWhileAliveDoesNothing()
    {
        var c = Hero();
        bool fired = false;
        c.Respawned += _ => fired = true;
        c.UpdateRespawn(10);
        Assert.False(fired);
    }
}
